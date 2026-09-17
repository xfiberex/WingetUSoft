using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Panel de estado de una tabla: cargando, sin datos todavía, sin coincidencias o con un error, con una acción
/// opcional que saca de ese estado.
/// </summary>
/// <remarks>
/// Extraído de <c>MainWindow</c> en F-18, igual que <see cref="ActivityLog"/>: las cuatro listas de la aplicación
/// (principal, Buscar, Desinstalar y Limpieza) dicen ahora lo mismo de la misma forma. Las otras tres solo
/// informaban en la barra de estado, así que una lista vacía era un hueco en blanco.
/// </remarks>
public sealed partial class ListStatePanel : UserControl
{
    /// <summary>Glifos de Segoe MDL2 Assets usados por el panel.</summary>
    public static class Glyph
    {
        public const string Sync = "";
        public const string CheckMark = "";
        public const string Search = "";
        public const string Warning = "";
    }

    /// <summary>Se dispara al pulsar el botón del estado (consultar, buscar, reintentar…).</summary>
    public event RoutedEventHandler? ActionInvoked;

    public ListStatePanel() => InitializeComponent();

    /// <summary>Muestra el estado de carga: anillo en marcha y sin acción.</summary>
    public void ShowLoading(string title, string body)
    {
        txtEstadoTitulo.Text = title;
        txtEstadoCuerpo.Text = body;
        ring.IsActive = true;
        ring.Visibility = Visibility.Visible;
        icon.Visibility = Visibility.Collapsed;
        btnEstadoAccion.Visibility = Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    /// <summary>Muestra un estado con su glifo y, si se le pasa texto, el botón que lo resuelve.</summary>
    public void Show(string title, string body, string glyph, string? actionText = null)
    {
        txtEstadoTitulo.Text = title;
        txtEstadoCuerpo.Text = body;
        ring.IsActive = false;
        ring.Visibility = Visibility.Collapsed;
        icon.Visibility = Visibility.Visible;
        icon.Glyph = glyph;

        btnEstadoAccion.Content = actionText ?? "";
        btnEstadoAccion.Visibility = string.IsNullOrEmpty(actionText) ? Visibility.Collapsed : Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    /// <summary>Oculta el panel y detiene el anillo: con filas en la tabla no hay estado que contar.</summary>
    public void Hide()
    {
        ring.IsActive = false;
        Visibility = Visibility.Collapsed;
    }

    private void BtnEstadoAccion_Click(object sender, RoutedEventArgs e) => ActionInvoked?.Invoke(this, e);
}
