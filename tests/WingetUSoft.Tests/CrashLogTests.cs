using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El registro de fallos tiene que **acumular**, no sustituir.
/// </summary>
/// <remarks>
/// El manejador escribía con <c>File.WriteAllText</c>: la segunda excepción borraba la evidencia de la
/// primera, y sin fecha no había forma de saber cuándo ocurrió ninguna de las dos. Cuando una app se
/// rompe en cadena, la primera excepción suele ser la causa y las siguientes el eco — quedarse solo con
/// la última es quedarse justo con la menos útil.
/// </remarks>
[Collection(DataDirectoryCollection.Name)]
public sealed class CrashLogTests : IDisposable
{
    private readonly string _testDataDirectory;

    public CrashLogTests()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "WingetUSoft.Tests", Guid.NewGuid().ToString("N"));
        AppSettings.DataDirectoryPath = _testDataDirectory;
    }

    public void Dispose()
    {
        AppSettings.DataDirectoryPath = AppSettings.DefaultDataDirectoryPath;
        if (Directory.Exists(_testDataDirectory))
            Directory.Delete(_testDataDirectory, recursive: true);
    }

    [Fact]
    public void Write_TwoConsecutiveFailures_KeepsBothDatedEntries()
    {
        CrashLog.Write("PrimeraVia", new InvalidOperationException("el fallo original"));
        CrashLog.Write("SegundaVia", new IOException("el eco"));

        string content = File.ReadAllText(CrashLog.FilePath);

        Assert.Contains("el fallo original", content);
        Assert.Contains("el eco", content);
        Assert.Contains("PrimeraVia", content);
        Assert.Contains("SegundaVia", content);

        // Dos entradas, cada una con su fecha: aaaa-mm-dd hh:mm:ss entre corchetes.
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(
            content, @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ",
            System.Text.RegularExpressions.RegexOptions.Multiline).Count);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesItInsteadOfThrowing()
    {
        Assert.False(Directory.Exists(_testDataDirectory));

        CrashLog.Write("Arranque", new InvalidOperationException("antes de que exista nada"));

        Assert.True(File.Exists(CrashLog.FilePath));
    }

    /// <summary>
    /// La lista de recuperables decide si la app sigue viva. Tragarse cualquier excepción la dejaba
    /// funcionando en un estado desconocido, que es peor que terminar.
    /// </summary>
    [Theory]
    [InlineData(typeof(OperationCanceledException), true)]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(UnauthorizedAccessException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(NullReferenceException), false)]
    [InlineData(typeof(InvalidOperationException), false)]
    [InlineData(typeof(OutOfMemoryException), false)]
    public void IsRecoverable_OnlyAcceptsTheKnownTypes(Type exceptionType, bool expected)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.Equal(expected, CrashLog.IsRecoverable(ex));
    }
}
