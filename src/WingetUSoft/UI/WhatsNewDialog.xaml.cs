using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

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

        var blocks = ReleaseNotes.ToBlocks(notesMarkdown, version);
        if (blocks.Count == 0) NotesText.Text = L.T("whatsnew.empty");
        else ShowBlocks(blocks);

        PrimaryButtonClick += async (_, _) =>
        {
            if (Uri.TryCreate(_url, UriKind.Absolute, out var uri))
                await Windows.System.Launcher.LaunchUriAsync(uri);
        };
    }

    /// <summary>
    /// Pinta las notas con los encabezados en seminegrita y separados de lo anterior por una línea en blanco
    /// (crítica de diseño, 2026-11-20): en texto plano, «Corregido» o «También» se leían como una frase más.
    /// </summary>
    private void ShowBlocks(IReadOnlyList<ReleaseNoteBlock> blocks)
    {
        var inlines = NotesText.Inlines;
        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (i > 0)
            {
                inlines.Add(new LineBreak());
                // Un encabezado pegado al párrafo anterior recibe su línea en blanco de separación.
                if (block.IsHeading && blocks[i - 1].Text.Length > 0) inlines.Add(new LineBreak());
            }

            if (block.Text.Length == 0) continue;
            inlines.Add(block.IsHeading
                ? new Run { Text = block.Text, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
                : new Run { Text = block.Text });
        }
    }
}
