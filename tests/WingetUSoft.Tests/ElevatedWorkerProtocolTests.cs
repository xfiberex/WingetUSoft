using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El protocolo del worker elevado (T2-15): un mensaje JSON por línea sobre una named pipe, con un
/// <c>hello</c> autenticado por token como primer mensaje obligatorio.
/// </summary>
/// <remarks>
/// Era la ruta más compleja del código y no tenía ni una prueba, justamente porque ejercitarla de
/// verdad exigía levantar un proceso elevado. Pero lo que decide la app —qué se dio por instalado, qué
/// falló, si el lote se cortó a mitad— sale de interpretar estas líneas, y eso es lógica pura en cuanto
/// se la separa de la tubería: aquí se le da un <see cref="StringReader"/> con la conversación entera.
/// </remarks>
public sealed class ElevatedWorkerProtocolTests
{
    private const string Token = "0123456789abcdef0123456789abcdef";

    private static Task<UpgradeBatchResult> ReadAsync(
        IEnumerable<string> lines,
        string authToken = Token,
        IProgress<UpgradeBatchStatusInfo>? statusProgress = null,
        IProgress<WingetProgressInfo>? downloadProgress = null)
        => WingetService.ReadElevatedWorkerMessagesAsync(
            new StringReader(string.Join("\n", lines)),
            authToken,
            statusProgress,
            downloadProgress,
            CancellationToken.None);

    private static string Hello(string token) => $$"""{"Type":"hello","Token":"{{token}}"}""";

    private static string Result(string id, bool success, int exitCode = 0) =>
        $$"""{"Type":"result","PackageId":"{{id}}","Success":{{(success ? "true" : "false")}},"ExitCode":{{exitCode}}}""";

    [Fact]
    public async Task Hello_WithTheRightToken_AuthenticatesAndTheBatchIsRead()
    {
        var result = await ReadAsync([Hello(Token), Result("Foo.Bar", success: true)]);

        Assert.Equal("", result.ErrorOutput);
        var item = Assert.Single(result.Items);
        Assert.Equal("Foo.Bar", item.PackageId);
        Assert.True(item.Result.Success);
    }

    /// <summary>
    /// Un token que no coincide no autentica **y no procesa nada**: ni siquiera los resultados que vengan
    /// detrás. Lo que llega por esa tubería decide lo que la app da por instalado.
    /// </summary>
    [Fact]
    public async Task Hello_WithTheWrongToken_YieldsAnUnauthenticatedResult()
    {
        var result = await ReadAsync([Hello("token-de-otro"), Result("Foo.Bar", success: true)]);

        Assert.Empty(result.Items);
        Assert.Equal(L.T("winget.elevatedAuthFailed"), result.ErrorOutput);
    }

    /// <summary>Sin <c>hello</c> previo no hay sesión, aunque el resto del protocolo sea impecable.</summary>
    [Fact]
    public async Task Messages_BeforeHello_AreRefused()
    {
        var result = await ReadAsync([Result("Foo.Bar", success: true), Hello(Token)]);

        Assert.Empty(result.Items);
        Assert.Equal(L.T("winget.elevatedAuthFailed"), result.ErrorOutput);
    }

    /// <summary>Un worker que nunca habla no es un lote vacío correcto: es una sesión sin comunicación.</summary>
    [Fact]
    public async Task NoMessagesAtAll_IsReportedAsNoCommunication()
    {
        var result = await ReadAsync([]);

        Assert.Empty(result.Items);
        Assert.Equal(L.T("winget.elevatedNoComm"), result.ErrorOutput);
    }

    /// <summary>
    /// El criterio central de la tarea: el worker corre elevado y cualquier ruido en su salida —una línea
    /// truncada, un mensaje a medio escribir— no puede costar el resultado del lote entero.
    /// </summary>
    [Theory]
    [InlineData("esto no es json")]
    [InlineData("{\"Type\":\"result\",")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[1,2,3]")]
    public async Task ACorruptLine_IsSkippedAndTheRestIsStillRead(string garbage)
    {
        var result = await ReadAsync(
        [
            Hello(Token),
            Result("Uno.Uno", success: true),
            garbage,
            Result("Dos.Dos", success: false, exitCode: 3),
        ]);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Uno.Uno", result.Items[0].PackageId);
        Assert.Equal("Dos.Dos", result.Items[1].PackageId);
        Assert.Equal(3, result.Items[1].Result.ExitCode);
    }

    [Fact]
    public async Task Result_CarriesEveryFieldOfTheUpgrade()
    {
        var result = await ReadAsync(
        [
            Hello(Token),
            """{"Type":"result","PackageId":"Foo.Bar","Success":false,"ExitCode":-1978335189,"UserCancelled":true,"Output":"salida","ErrorOutput":"fallo"}""",
        ]);

        var item = Assert.Single(result.Items);
        Assert.Equal("Foo.Bar", item.PackageId);
        Assert.False(item.Result.Success);
        Assert.Equal(-1978335189, item.Result.ExitCode);
        Assert.True(item.Result.UserCancelled);
        Assert.Equal("salida", item.Result.Output);
        Assert.Equal("fallo", item.Result.ErrorOutput);
    }

    /// <summary>
    /// Cancelar a mitad de lote: los paquetes ya hechos se conservan y el resumen marca que se cortó.
    /// Perder ese distintivo haría que la app presentara un lote incompleto como si hubiera terminado.
    /// </summary>
    [Fact]
    public async Task Summary_ReportsABatchCancelledMidway()
    {
        var result = await ReadAsync(
        [
            Hello(Token),
            Result("Uno.Uno", success: true),
            Result("Dos.Dos", success: true),
            """{"Type":"summary","BatchCancelled":true,"ErrorOutput":"cancelado por el usuario"}""",
        ]);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.CancelledAfterCurrentPackage);
        Assert.Equal("cancelado por el usuario", result.ErrorOutput);
    }

    [Fact]
    public async Task Summary_OfACompleteBatch_DoesNotMarkItCancelled()
    {
        var result = await ReadAsync(
        [
            Hello(Token),
            Result("Uno.Uno", success: true),
            """{"Type":"summary","BatchCancelled":false}""",
        ]);

        Assert.False(result.CancelledAfterCurrentPackage);
        Assert.Equal("", result.ErrorOutput);
    }

    [Fact]
    public async Task Status_IsForwardedToTheProgressReporter()
    {
        var reported = new List<UpgradeBatchStatusInfo>();

        await ReadAsync(
        [
            Hello(Token),
            """{"Type":"status","Phase":"upgrading","PackageId":"Foo.Bar","CurrentIndex":2,"TotalCount":5}""",
        ], statusProgress: new Progress<UpgradeBatchStatusInfo>(reported.Add));

        // Progress<T> despacha por el contexto de sincronización: en un test de consola, el pool.
        await WaitUntil(() => reported.Count == 1);

        Assert.Equal("upgrading", reported[0].Phase);
        Assert.Equal("Foo.Bar", reported[0].PackageId);
        Assert.Equal(2, reported[0].CurrentIndex);
        Assert.Equal(5, reported[0].TotalCount);
    }

    /// <summary>
    /// Un <c>progress</c> sin tamaño total no se retransmite: la barra de descarga no puede calcular
    /// porcentaje ni ETA con un denominador de cero, y mostraría un progreso inventado.
    /// </summary>
    [Fact]
    public async Task Progress_WithoutATotalSize_IsNotForwarded()
    {
        var reported = new List<WingetProgressInfo>();

        await ReadAsync(
        [
            Hello(Token),
            """{"Type":"progress","DownloadedBytes":1024,"TotalBytes":0,"SpeedBytesPerSecond":512}""",
            """{"Type":"progress","DownloadedBytes":2048,"TotalBytes":8192,"SpeedBytesPerSecond":512}""",
        ], downloadProgress: new Progress<WingetProgressInfo>(reported.Add));

        await WaitUntil(() => reported.Count == 1);
        await Task.Delay(50);

        var only = Assert.Single(reported);
        Assert.Equal(8192, only.TotalBytes);
    }

    /// <summary>Un tipo de mensaje que esta versión no conoce se ignora sin romper la sesión.</summary>
    [Fact]
    public async Task AnUnknownMessageType_IsIgnored()
    {
        var result = await ReadAsync(
        [
            Hello(Token),
            """{"Type":"telemetria-del-futuro","PackageId":"Foo.Bar"}""",
            Result("Foo.Bar", success: true),
        ]);

        Assert.Single(result.Items);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
            await Task.Delay(10);
    }
}
