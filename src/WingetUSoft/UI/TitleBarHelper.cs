using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace WingetUSoft;

internal static class TitleBarHelper
{
    internal static void UpdateButtonColors(AppWindow appWindow, UIElement content, int themeModeFallback)
    {
        if (appWindow?.TitleBar is not { } titleBar) return;

        bool isDark = content is FrameworkElement fe
            ? fe.ActualTheme == ElementTheme.Dark
            : themeModeFallback == 2;

        titleBar.ButtonBackgroundColor         = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (isDark)
        {
            titleBar.ButtonForegroundColor         = Colors.White;
            titleBar.ButtonHoverForegroundColor    = Colors.White;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor  = Colors.White;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 255, 255, 255);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonForegroundColor         = Colors.Black;
            titleBar.ButtonHoverForegroundColor    = Colors.Black;
            titleBar.ButtonHoverBackgroundColor    = Windows.UI.Color.FromArgb(32, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor  = Colors.Black;
            titleBar.ButtonPressedBackgroundColor  = Windows.UI.Color.FromArgb(16, 0, 0, 0);
            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(128, 0, 0, 0);
        }
    }
}
