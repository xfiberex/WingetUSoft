using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogos compartidos por <c>MainWindow</c>, <c>SearchWindow</c>, <c>UninstallWindow</c> y
/// <c>CleanupWindow</c>, es decir, prácticamente todos los de la aplicación.
/// </summary>
/// <remarks>
/// Los textos de los botones se resuelven con <see cref="L.T"/> **en cada llamada**, no como valor por
/// defecto del parámetro: un valor por defecto tiene que ser constante en tiempo de compilación, así que
/// un literal aquí se quedaría fijo en el idioma en que se escribió. Eso es justo lo que pasaba hasta la
/// auditoría del 2026-08-20 — «Aceptar», «Sí» y «No» estaban cableados en español y se mostraban tal cual
/// con la interfaz en inglés, portugués, francés o italiano, donde «No» ni siquiera es palabra francesa.
/// De ahí el <c>null</c> como valor por defecto: significa "el texto estándar del idioma actual".
/// </remarks>
internal static class WindowDialogHelper
{
    internal static async Task ShowDialogAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title           = title,
            Content         = message,
            CloseButtonText = L.T("btn.accept"),
            XamlRoot        = xamlRoot
        };
        await dialog.ShowAsync();
    }

    internal static async Task<bool> ShowConfirmDialogAsync(
        XamlRoot xamlRoot,
        string title,
        string message,
        string? primaryText = null,
        string? closeText   = null)
    {
        var dialog = new ContentDialog
        {
            Title             = title,
            Content           = message,
            PrimaryButtonText = primaryText ?? L.T("btn.yes"),
            CloseButtonText   = closeText   ?? L.T("btn.no"),
            DefaultButton     = ContentDialogButton.Primary,
            XamlRoot          = xamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
