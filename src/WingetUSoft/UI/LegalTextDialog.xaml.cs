using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogo genérico para mostrar un texto legal extenso con scroll y selección (licencia MIT,
/// avisos de terceros). El contenido se pasa ya resuelto desde <see cref="LegalText"/>.
/// </summary>
public sealed partial class LegalTextDialog : ContentDialog
{
    /// <summary>Crea el diálogo con un título y el texto a mostrar.</summary>
    /// <param name="title">Título del diálogo (p. ej. "Licencia").</param>
    /// <param name="body">Texto a mostrar; si viene vacío se muestra un aviso de no disponible.</param>
    public LegalTextDialog(string title, string body)
    {
        InitializeComponent();
        Title           = title;
        CloseButtonText = L.T("btn.close");
        BodyText.Text   = string.IsNullOrWhiteSpace(body) ? L.T("legal.unavailable") : body;
    }
}
