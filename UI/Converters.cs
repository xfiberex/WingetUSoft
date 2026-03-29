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

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is not true;
}

public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? 0.4 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
