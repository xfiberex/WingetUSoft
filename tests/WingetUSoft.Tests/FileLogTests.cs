using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Escritura del registro a disco fuera del hilo de UI (T2-01) y purga por antigüedad (T2-02).
/// </summary>
[Collection(DataDirectoryCollection.Name)]
public sealed class FileLogTests : IDisposable
{
    private readonly string _testDataDirectory;

    public FileLogTests()
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

    private static string TodaysLog() =>
        Path.Combine(AppSettings.LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

    /// <summary>
    /// Lo que de verdad arregla T2-01: una ráfaga entera abre el archivo **una vez**, no una por línea.
    /// </summary>
    /// <remarks>
    /// El código anterior hacía <c>File.AppendAllText</c> por línea —abrir, escribir y cerrar— en el
    /// hilo de UI. Un lote de diez paquetes retransmite toda la salida de winget: cientos de aperturas,
    /// cada una parando el hilo que tiene que repintar el progreso.
    /// </remarks>
    [Fact]
    public void Write_ABurstOfLines_OpensTheFileOnce()
    {
        using var log = new FileLog(_ => { });

        for (int i = 0; i < 200; i++)
            log.Write($"línea {i}");

        log.Dispose();

        Assert.Equal(1, log.FileOpenCount);
        Assert.Equal(200, File.ReadAllLines(TodaysLog()).Length);
    }

    /// <summary>El formato en disco no cambia respecto al que escribía la versión síncrona.</summary>
    [Fact]
    public void Write_KeepsTheSameLineFormat_AsTheSynchronousVersion()
    {
        using (var log = new FileLog(_ => { }))
        {
            log.Write("Consultando actualizaciones");
            log.Dispose();
        }

        string line = File.ReadAllLines(TodaysLog()).Single();

        Assert.Matches(@"^\[\d{2}:\d{2}:\d{2}\] Consultando actualizaciones$", line);
    }

    /// <summary>Un único consumidor: las líneas llegan al archivo en el mismo orden en que se encolaron.</summary>
    [Fact]
    public void Write_PreservesOrder()
    {
        using (var log = new FileLog(_ => { }))
        {
            for (int i = 0; i < 50; i++) log.Write($"#{i}");
            log.Dispose();
        }

        string[] lines = File.ReadAllLines(TodaysLog());

        for (int i = 0; i < 50; i++)
            Assert.EndsWith($"#{i}", lines[i], StringComparison.Ordinal);
    }

    /// <summary>Dispose no puede perder lo encolado: es lo último que se escribe al cerrar la app.</summary>
    [Fact]
    public void Dispose_FlushesWhatWasStillQueued()
    {
        var log = new FileLog(_ => { });
        log.Write("lo último que pasó");
        log.Dispose();

        Assert.Contains("lo último que pasó", File.ReadAllText(TodaysLog()), StringComparison.Ordinal);
    }

    /// <summary>
    /// Si el disco falla, el registro se apaga y **avisa una sola vez**, sin tirar la app abajo.
    /// </summary>
    [Fact]
    public void Write_WhenTheLogPathIsUnusable_ReportsOnceAndStaysQuiet()
    {
        // Un archivo donde debería ir el directorio de logs: CreateDirectory no puede con esto.
        Directory.CreateDirectory(_testDataDirectory);
        File.WriteAllText(AppSettings.LogDirectory, "ocupado");

        int failures = 0;
        using var log = new FileLog(_ => Interlocked.Increment(ref failures));

        log.Write("una");
        log.Write("otra");
        log.Dispose();

        Assert.False(log.IsAvailable);
        Assert.Equal(1, failures);
    }

    // ── Purga por antigüedad (T2-02) ───────────────────────────────────────

    [Fact]
    public void PurgeOldLogs_KeepsTheLastThirtyDays_AndDeletesTheRest()
    {
        Directory.CreateDirectory(AppSettings.LogDirectory);

        for (int daysAgo = 0; daysAgo < 40; daysAgo++)
        {
            string name = $"{DateTime.Today.AddDays(-daysAgo):yyyy-MM-dd}.log";
            File.WriteAllText(Path.Combine(AppSettings.LogDirectory, name), "x");
        }

        int deleted = AppSettings.PurgeOldLogs();

        string[] remaining = Directory.GetFiles(AppSettings.LogDirectory, "*.log");
        Assert.Equal(30, remaining.Length);
        Assert.Equal(10, deleted);

        // El más viejo que sobrevive es el de hace 29 días; el de hace 30 ya se fue.
        Assert.Contains(remaining, f => Path.GetFileName(f) == $"{DateTime.Today.AddDays(-29):yyyy-MM-dd}.log");
        Assert.DoesNotContain(remaining, f => Path.GetFileName(f) == $"{DateTime.Today.AddDays(-30):yyyy-MM-dd}.log");
    }

    /// <summary>
    /// Solo se borra lo que escribimos nosotros: un archivo con otro nombre no se toca aunque sea viejo.
    /// </summary>
    [Fact]
    public void PurgeOldLogs_LeavesFilesItDidNotWrite()
    {
        Directory.CreateDirectory(AppSettings.LogDirectory);
        string foreign = Path.Combine(AppSettings.LogDirectory, "notas-del-usuario.log");
        File.WriteAllText(foreign, "no es nuestro");
        File.SetLastWriteTime(foreign, DateTime.Now.AddYears(-2));

        AppSettings.PurgeOldLogs();

        Assert.True(File.Exists(foreign));
    }

    /// <summary>Sin carpeta de logs no hay nada que purgar, y desde luego no un fallo al arrancar.</summary>
    [Fact]
    public void PurgeOldLogs_WithoutALogDirectory_DoesNothing()
    {
        Assert.False(Directory.Exists(AppSettings.LogDirectory));
        Assert.Equal(0, AppSettings.PurgeOldLogs());
    }
}
