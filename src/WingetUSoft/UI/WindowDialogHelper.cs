using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

internal static class WindowDialogHelper
{
    internal static async Task ShowDialogAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title           = title,
            Content         = message,
            CloseButtonText = "Aceptar",
            XamlRoot        = xamlRoot
        };
        await dialog.ShowAsync();
    }

    internal static async Task<bool> ShowConfirmDialogAsync(
        XamlRoot xamlRoot,
        string title,
        string message,
        string primaryText = "Sí",
        string closeText   = "No")
    {
        var dialog = new ContentDialog
        {
            Title             = title,
            Content           = message,
            PrimaryButtonText = primaryText,
            CloseButtonText   = closeText,
            DefaultButton     = ContentDialogButton.Primary,
            XamlRoot          = xamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
