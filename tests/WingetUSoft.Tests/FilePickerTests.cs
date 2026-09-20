using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Selectores de archivo que funcionan con la aplicación elevada (F-27).
/// </summary>
/// <remarks>
/// Los de <c>Windows.Storage.Pickers</c> lanzan <c>COMException 0x80004005</c> en un proceso elevado, y
/// <c>CrashLog.IsRecoverable</c> no perdona esa excepción: exportar o importar no hacía nada y dejaba una entrada
/// en <c>crash.log</c>. Los de Windows App SDK (<c>Microsoft.Windows.Storage.Pickers</c>) reciben el
/// <c>WindowId</c>, admiten procesos elevados y devuelven una ruta en vez de un <c>StorageFile</c>.
/// </remarks>
public sealed class FilePickerTests
{
    [Fact]
    public void NoCode_UsesTheWindowsStoragePickers()
    {
        var offenders = SourceFiles()
            .Where(f => File.ReadAllText(f).Contains("using Windows.Storage.Pickers;", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(offenders.Count == 0,
            "Archivos con los selectores que fallan al elevar: " + string.Join(", ", offenders));
    }

    /// <summary>Cada selector recibe la ventana por su <c>WindowId</c>; el interop antiguo ya no hace falta.</summary>
    [Theory]
    [InlineData("UI/MainWindow.Menus.cs", 3)]     // exportar lista, exportar paquetes, importar paquetes
    [InlineData("UI/HistoryWindow.xaml.cs", 1)]   // exportar CSV
    public void EveryPicker_TakesTheWindowId(string file, int pickers)
    {
        string code = File.ReadAllText(Path.Combine(SourceDirectory(), file.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Equal(pickers, Occurrences(code, "Picker(AppWindow.Id)"));
        Assert.DoesNotContain("InitializeWithWindow.Initialize(picker", code, StringComparison.Ordinal);
    }

    /// <summary>Lo exportado se escribe con BOM: es lo que hacía el API anterior y lo que Excel necesita.</summary>
    [Fact]
    public void ExportedFiles_KeepTheirByteOrderMark()
    {
        string code = File.ReadAllText(Path.Combine(SourceDirectory(), "Core", "FileExport.cs"));

        Assert.Contains("encoderShouldEmitUTF8Identifier: true", code, StringComparison.Ordinal);
        Assert.Equal(2, SourceFiles().Count(f => File.ReadAllText(f).Contains("FileExport.WriteTextWithBomAsync", StringComparison.Ordinal)));
    }

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(SourceDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal));

    private static string SourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft");
    }
}
