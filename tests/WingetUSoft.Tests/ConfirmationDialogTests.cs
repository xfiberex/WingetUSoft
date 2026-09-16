using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Confirmaciones con el verbo de la acción y sin un Intro que ejecute lo irreversible (F-04).
/// </summary>
/// <remarks>
/// Hasta F-04 todas las confirmaciones respondían «Sí / No» y el primario era siempre el botón por defecto:
/// con el foco inicial en «Sí», un Intro desinstalaba un programa o borraba carpetas de forma recursiva. Se
/// fija aquí y no en los UI tests porque probarlo conduciendo la app obligaría a abrir la confirmación de
/// desinstalar de verdad, y si el arreglo se revirtiera el test desinstalaría un programa del equipo.
/// </remarks>
public sealed class ConfirmationDialogTests
{
    [Fact]
    public void DestructiveConfirmation_DefaultsToCancel()
    {
        Assert.Equal(ContentDialogButton.Close, WindowDialogHelper.DefaultButtonFor(destructive: true));
        Assert.Equal(ContentDialogButton.Primary, WindowDialogHelper.DefaultButtonFor(destructive: false));
    }

    /// <summary>
    /// Desinstalar y borrar residuos son las dos acciones que no se pueden deshacer: sus confirmaciones tienen
    /// que declararse destructivas en la propia llamada.
    /// </summary>
    [Theory]
    [InlineData("UninstallWindow.xaml.cs", "uninstall.confirmPrimary")]
    [InlineData("CleanupWindow.xaml.cs", "cleanup.confirmPrimary")]
    public void IrreversibleActions_ConfirmAsDestructive(string file, string verbKey)
    {
        string code = File.ReadAllText(Path.Combine(UiDirectory(), file));
        var call = new Regex(
            @"ShowConfirmDialogAsync\((?<args>[^;]*)\);", RegexOptions.Singleline);

        var calls = call.Matches(code).Select(m => m.Groups["args"].Value).ToList();

        Assert.Single(calls);
        Assert.Contains($"L.T(\"{verbKey}\")", calls[0], StringComparison.Ordinal);
        Assert.Matches(@"destructive:\s*true", calls[0]);
    }

    /// <summary>«Sí», «No» y «Sí, eliminar» ya no existen: ninguna confirmación puede volver a usarlos.</summary>
    [Theory]
    [InlineData("btn.yes")]
    [InlineData("btn.no")]
    [InlineData("btn.yesDelete")]
    public void YesNoButtonTexts_AreGone(string key) =>
        Assert.False(L.Map.ContainsKey(key), $"La clave '{key}' volvió al diccionario.");

    private static string UiDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx) desde " + AppContext.BaseDirectory);
        return Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
    }
}
