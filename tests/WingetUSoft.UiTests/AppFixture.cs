using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace WingetUSoft.UiTests;

public sealed class AppFixture : IDisposable
{
    public Application App { get; }
    public UIA3Automation Automation { get; }
    public Window MainWindow { get; }

    private readonly SettingsBackup _settingsBackup;

    public AppFixture()
    {
        // Diferencia clave con FormatDiskPro: WingetUSoft.exe corre asInvoker (ver app.manifest), no
        // requireAdministrator. Winget se invoca bajo demanda vía un worker elevado por named pipe
        // (Services/WingetService.cs), solo cuando el propio usuario lo pide desde la UI -- el proceso
        // de test NO necesita una terminal elevada para automatizar esta ventana. Por eso, a diferencia
        // de FormatDiskPro.UiTests, aquí no hay ningún EnsureElevated().

        // La app es unpackaged: no tiene almacenamiento aislado por prueba. settings.json (con el
        // historial dentro), su .bak y los registros viven en %LocalAppData%\WingetUSoft, el MISMO sitio
        // que usa la instalación real del usuario. Sin este backup, cambiar idioma/opciones durante las
        // pruebas dejaría esos cambios filtrados en la app de verdad (F-25).
        _settingsBackup = SettingsBackup.Capture();

        var exePath = ResolveExePath();
        App = Application.Launch(exePath);
        Automation = new UIA3Automation();
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(20))
            ?? throw new InvalidOperationException("WingetUSoft no abrió su ventana principal a tiempo.");

        // Al primer arranque de una versión, MainWindow dispara por su cuenta MaybeShowWhatsNewAsync()
        // y CheckForAppUpdateAsync() en el Loaded del contenido raíz -- pueden abrir un ContentDialog
        // (Novedades / Actualización disponible) en cualquier momento de los primeros segundos, sin
        // relación con lo que esté haciendo un test. WinUI solo permite un ContentDialog abierto a la
        // vez: si esto colisiona con el diálogo que abre un test, la app intenta abrir un segundo
        // dentro de un manejador async void sin captura y el proceso muere. Se descarta aquí, antes de
        // que arranque ningún test.
        DialogHelper.DismissStartupDialogs(this, TimeSpan.FromSeconds(8));
    }

    public void Dispose()
    {
        try { App.Close(); } catch { /* pudo cerrarse ya dentro de un test */ }
        WaitForAppExit();
        Automation.Dispose();
        App.Dispose();
        _settingsBackup.Restore();
    }

    /// <summary>
    /// La restauración va DESPUÉS de que el proceso termine: al cerrarse, la app todavía vacía su registro
    /// a disco (<c>FileLog.Dispose</c>), y una escritura tardía pisaría lo recién restaurado.
    /// </summary>
    private void WaitForAppExit()
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(App.ProcessId);
            if (!process.WaitForExit(15_000))
            {
                process.Kill();
                process.WaitForExit(5_000);
            }
        }
        catch (ArgumentException) { /* el proceso ya no existe */ }
        catch (InvalidOperationException) { /* terminó entre la búsqueda y la espera */ }
    }

    private static string ResolveExePath()
    {
        var overridePath = Environment.GetEnvironmentVariable("WINGETUSOFT_EXE");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException($"WINGETUSOFT_EXE apunta a una ruta inexistente: {overridePath}");
            return overridePath;
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException(
                $"No se encontró la raíz del repo (WingetUSoft.slnx) subiendo desde {AppContext.BaseDirectory}.");

        var binRoot = Path.Combine(repoRoot, "src", "WingetUSoft", "bin");
        var candidates = Directory.Exists(binRoot)
            ? Directory.GetFiles(binRoot, "WingetUSoft.exe", SearchOption.AllDirectories)
            : [];

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException(
                "No se encontró WingetUSoft.exe compilado. Ejecuta 'dotnet build' (o 'dotnet publish') sobre " +
                "src/WingetUSoft antes de correr estas pruebas, o define WINGETUSOFT_EXE con la ruta al ejecutable.");
        }

        return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    internal static string? FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "WingetUSoft app";
}
