using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WingetUSoft;

/// <summary>
/// Fila de Configuración con el patrón de la Configuración de Windows: título y descripción a la izquierda y el
/// control a la derecha (F-16).
/// </summary>
/// <remarks>
/// <para>
/// Hasta F-16 la página mezclaba cuatro formas de rotular: radios con subtítulo, <c>ComboBox</c> con
/// <c>Header</c>, <c>CheckBox</c> con el texto al lado e interruptores con un <c>TextBlock</c> suelto. Es la misma
/// idea que el <c>SettingsCard</c> del Community Toolkit, sin añadir esa dependencia (como <c>WrapPanel</c>).
/// </para>
/// <para>
/// La plantilla vive en <c>Styles.xaml</c>. El control de la derecha se nombra desde el título visible
/// (<c>LabeledBy</c>), así que un lector de pantalla anuncia lo mismo que se ve, en los cinco idiomas, sin claves
/// de traducción aparte. El título expone el <c>AutomationId</c> «<c>x:Name</c> de la fila + <c>Header</c>» para
/// que los UI tests puedan compararlo con el nombre del control.
/// </para>
/// </remarks>
public sealed partial class SettingsCard : ContentControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(""));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(""));

    private TextBlock? _headerText;

    /// <summary>Título de la opción.</summary>
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Qué hace la opción y, si las tiene, sus consecuencias.</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _headerText = GetTemplateChild("HeaderText") as TextBlock;
        if (_headerText is not null && !string.IsNullOrEmpty(Name))
            AutomationProperties.SetAutomationId(_headerText, Name + "Header");
        LabelContent();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        LabelContent();
    }

    /// <summary>
    /// Nombra el control desde el título, salvo que sea un botón: un botón ya dice lo que hace («Abrir carpeta»,
    /// «Limpiar lista»), y <c>LabeledBy</c> taparía ese texto con el título de la fila.
    /// </summary>
    private void LabelContent()
    {
        if (_headerText is not null && Content is UIElement control and not ButtonBase)
            AutomationProperties.SetLabeledBy(control, _headerText);
    }
}
