using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace WingetUSoft;

/// <summary>
/// Panel de envoltura nativo, sin dependencias externas: coloca los hijos en fila y salta a la
/// siguiente cuando no caben en el ancho disponible. Usado en la barra de "Acciones rápidas" de
/// <c>MainWindow</c> para que los botones bajen de fila (en vez de recortarse) al estrechar la
/// ventana o con fuentes más largas (p. ej. FR/IT).
/// </summary>
public sealed partial class WrapPanel : Panel
{
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(WrapPanel),
            new PropertyMetadata(0.0, OnSpacingChanged));

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(WrapPanel),
            new PropertyMetadata(0.0, OnSpacingChanged));

    /// <summary>Espacio horizontal entre elementos de la misma fila.</summary>
    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    /// <summary>Espacio vertical entre filas.</summary>
    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((WrapPanel)d).InvalidateMeasure();

    protected override Size MeasureOverride(Size availableSize)
    {
        double rowWidth = 0;
        double rowHeight = 0;
        double totalWidth = 0;
        double totalHeight = 0;
        bool rowHasItems = false;

        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            Size size = child.DesiredSize;

            double neededWidth = rowHasItems ? rowWidth + HorizontalSpacing + size.Width : size.Width;

            if (rowHasItems && neededWidth > availableSize.Width)
            {
                // No cabe en la fila actual: cierra la fila y empieza una nueva con este hijo.
                totalWidth = Math.Max(totalWidth, rowWidth);
                totalHeight += rowHeight + (totalHeight > 0 ? VerticalSpacing : 0);
                rowWidth = size.Width;
                rowHeight = size.Height;
            }
            else
            {
                rowWidth = neededWidth;
                rowHeight = Math.Max(rowHeight, size.Height);
                rowHasItems = true;
            }
        }

        if (rowHasItems)
        {
            totalWidth = Math.Max(totalWidth, rowWidth);
            totalHeight += rowHeight + (totalHeight > 0 ? VerticalSpacing : 0);
        }

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        double y = 0;
        double rowHeight = 0;
        bool rowHasItems = false;

        foreach (var child in Children)
        {
            Size size = child.DesiredSize;
            double neededWidth = rowHasItems ? x + HorizontalSpacing + size.Width : size.Width;

            if (rowHasItems && neededWidth > finalSize.Width)
            {
                // Salto de fila.
                y += rowHeight + VerticalSpacing;
                x = 0;
                rowHeight = 0;
                rowHasItems = false;
                neededWidth = size.Width;
            }

            double itemX = rowHasItems ? x + HorizontalSpacing : x;
            child.Arrange(new Rect(itemX, y, size.Width, size.Height));

            x = itemX + size.Width;
            rowHeight = Math.Max(rowHeight, size.Height);
            rowHasItems = true;
        }

        return finalSize;
    }
}
