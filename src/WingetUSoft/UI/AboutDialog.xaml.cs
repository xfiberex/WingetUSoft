using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogo "Acerca de": descripción, versión, copyright/licencia (MIT) y aviso de privacidad.
/// Incluye accesos al repositorio en GitHub y a la página de donaciones.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    private const string RepoUrl = "https://github.com/xfiberex/WingetUSoft";

    /// <summary>
    /// Enlace de donación (PayPal). Apoyar el proyecto es <b>voluntario</b>: no desbloquea nada ni cambia lo
    /// que hace la aplicación. Si queda vacío, el botón no se muestra.
    /// </summary>
    private const string DonateUrl = "https://www.paypal.me/RJimenez1820";

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

        if (!string.IsNullOrEmpty(DonateUrl))
            PrimaryButtonText = L.T("about.donate");
        SecondaryButtonText = L.T("about.github");
        CloseButtonText     = L.T("btn.close");
        // Ni apoyar el proyecto ni ver el repositorio son la acción principal de Acerca de: el acento (y el
        // Intro) van a Cerrar (F-12).
        DefaultButton       = ContentDialogButton.Close;

        // Abrir los enlaces sin cerrar el diálogo.
        PrimaryButtonClick   += async (_, args) => { args.Cancel = true; await OpenAsync(DonateUrl); };
        SecondaryButtonClick += async (_, args) => { args.Cancel = true; await OpenAsync(RepoUrl); };
    }

    private static async Task OpenAsync(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Windows.System.Launcher.LaunchUriAsync(uri);
    }
}
