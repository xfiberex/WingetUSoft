using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WingetUSoft;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool boolVal = value is true;
        if (parameter is string s && s == "Invert")
            boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        bool vis = value is Visibility v && v == Visibility.Visible;
        if (parameter is string s && s == "Invert")
            vis = !vis;
        return vis;
    }
}

/// <summary>
/// Elige entre dos objetos declarados en XAML según un booleano; p. ej. el estilo normal o el atenuado de
/// una celda (F-06).
/// </summary>
/// <remarks>
/// Sustituye a <c>BoolToOpacityConverter</c>: atenuar con <c>Opacity</c> hundía el contraste del texto por
/// debajo de WCAG AA. Los valores son objetos ya creados en XAML —no pinceles resueltos aquí—, así que un
/// estilo con <c>{ThemeResource}</c> sigue el tema de la ventana en la que se aplica.
/// </remarks>
public sealed class BoolToObjectConverter : IValueConverter
{
    public object? TrueValue { get; set; }

    public object? FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language) =>
        (value is true ? TrueValue : FalseValue)!;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
