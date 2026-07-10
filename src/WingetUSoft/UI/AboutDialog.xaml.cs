using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogo "Acerca de": descripción, versión, copyright/licencia (MIT) y aviso de privacidad.
/// Incluye un acceso directo al repositorio en GitHub.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    private const string RepoUrl = "https://github.com/xfiberex/WingetUSoft";

    public AboutDialog()
    {
        InitializeComponent();

        var appVer = typeof(AboutDialog).Assembly.GetName().Version;
        string version = appVer is not null ? $"{appVer.Major}.{appVer.Minor}.{appVer.Build}" : "";

        Title              = L.T("about.title");
        VersionText.Text   = L.T("about.version", version);
        DescText.Text      = L.T("about.desc");
        CopyrightText.Text = L.T("about.copyright");
        PrivacyHeader.Text = L.T("about.privacyHeader");
        PrivacyText.Text   = L.T("about.privacy");

        PrimaryButtonText = L.T("about.github");
        CloseButtonText   = L.T("btn.close");

        // Abrir el repositorio sin cerrar el diálogo.
        PrimaryButtonClick += async (_, args) =>
        {
            args.Cancel = true;
            if (Uri.TryCreate(RepoUrl, UriKind.Absolute, out var uri))
                await Windows.System.Launcher.LaunchUriAsync(uri);
        };
    }
}
