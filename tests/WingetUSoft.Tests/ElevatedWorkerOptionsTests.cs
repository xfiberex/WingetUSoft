using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Los argumentos con los que se invoca al worker elevado (T3-16). Es la única frontera entre una
/// línea de comandos y un proceso que va a correr como administrador, así que lo que no valide aquí
/// no lo valida nadie.
/// </summary>
public sealed class ElevatedWorkerOptionsTests
{
    private const string ValidCancelEvent = @"Local\WingetUSoft.Cancel.0123456789abcdef";

    private static string[] Args(string cancelEventName) =>
    [
        "--elevated-batch-worker",
        "--pipe-name", "WingetUSoft.abc123",
        "--auth-token", "0123456789abcdef",
        "--cancel-event", cancelEventName,
        "--package-id", "Foo.Bar",
        "--silent",
    ];

    [Fact]
    public void AValidInvocation_IsParsed()
    {
        var options = WingetService.ParseElevatedWorkerOptions(Args(ValidCancelEvent));

        Assert.Equal("WingetUSoft.abc123", options.PipeName);
        Assert.Equal("0123456789abcdef", options.AuthToken);
        Assert.Equal(ValidCancelEvent, options.CancelEventName);
        Assert.Equal(["Foo.Bar"], options.PackageIds);
        Assert.True(options.Silent);
    }

    /// <summary>
    /// El nombre acaba en <c>EventWaitHandle.OpenExisting</c>, que abriría cualquier objeto de
    /// sincronización del sistema que se le nombre. Exigir el prefijo lo acota a los que crea esta app.
    /// </summary>
    /// <remarks>
    /// No es explotable por sí solo —quien controla los argumentos del worker ya ejecuta código como el
    /// usuario— pero el worker corre **elevado**, y ahí el listón de lo que se acepta sin mirar es otro.
    /// </remarks>
    [Theory]
    [InlineData(@"Global\Cualquier.Otro.Evento")]
    [InlineData(@"Local\OtraApp.Cancel.abc")]
    [InlineData(@"Local\WingetUSoft.Otro.abc")]
    [InlineData("WingetUSoft.Cancel.abc")]                  // sin el espacio de nombres Local\
    [InlineData(@"local\WingetUSoft.Cancel.abc")]           // el prefijo distingue mayúsculas
    public void ACancelEventOutsideTheExpectedPrefix_IsRefused(string cancelEventName)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => WingetService.ParseElevatedWorkerOptions(Args(cancelEventName)));

        Assert.Contains(@"Local\WingetUSoft.Cancel.", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--pipe-name")]
    [InlineData("--auth-token")]
    [InlineData("--cancel-event")]
    [InlineData("--package-id")]
    public void AMissingRequiredArgument_IsRefused(string argumentToDrop)
    {
        var trimmed = new List<string>();
        string[] full = Args(ValidCancelEvent);
        for (int i = 0; i < full.Length; i++)
        {
            if (full[i] == argumentToDrop)
            {
                i++;   // se salta también su valor
                continue;
            }

            trimmed.Add(full[i]);
        }

        Assert.Throws<ArgumentException>(() => WingetService.ParseElevatedWorkerOptions([.. trimmed]));
    }

    [Fact]
    public void AnUnknownArgument_IsRefused()
    {
        string[] args = [.. Args(ValidCancelEvent), "--formatear-el-disco"];

        Assert.Throws<ArgumentException>(() => WingetService.ParseElevatedWorkerOptions(args));
    }
}
