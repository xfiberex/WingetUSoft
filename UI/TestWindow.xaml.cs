using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

public sealed partial class TestWindow : Window
{
    public TestWindow()
    {
        InitializeComponent();
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }
}
