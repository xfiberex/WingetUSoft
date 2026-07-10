using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogo de novedades: muestra las notas de la versión instalada (cuerpo del release de GitHub,
/// convertido a texto plano por <see cref="ReleaseNotes.ToPlainText"/>). Se abre automáticamente una
/// sola vez tras una actualización y también bajo demanda desde <em>Ayuda → Novedades…</em>.
/// </summary>
public sealed partial class WhatsNewDialog : ContentDialog
{
    private readonly string _url;

    /// <summary>Crea el diálogo para una versión, sus notas Markdown y la URL del release.</summary>
    /// <param name="version">Versión legible a mostrar (p. ej. "1.3.0").</param>
    /// <param name="notesMarkdown">Cuerpo Markdown de las notas (puede venir vacío).</param>
    /// <param name="url">URL del release en GitHub (botón "Ver en GitHub").</param>
    public WhatsNewDialog(string version, string notesMarkdown, string url)
    {
        InitializeComponent();
        _url = url;

        Title             = L.T("whatsnew.title");
        VersionText.Text  = L.T("whatsnew.version", version);
        PrimaryButtonText = L.T("whatsnew.viewOnGitHub");
        CloseButtonText   = L.T("btn.close");
        DefaultButton     = ContentDialogButton.Close;

        string plain = ReleaseNotes.ToPlainText(notesMarkdown);
        NotesText.Text = string.IsNullOrWhiteSpace(plain) ? L.T("whatsnew.empty") : plain;

        PrimaryButtonClick += async (_, _) =>
        {
            if (Uri.TryCreate(_url, UriKind.Absolute, out var uri))
                await Windows.System.Launcher.LaunchUriAsync(uri);
        };
    }
}
