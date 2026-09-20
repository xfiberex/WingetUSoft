using Windows.UI.ViewManagement;

namespace WingetUSoft;

/// <summary>
/// Si hay un tema de contraste de Windows activo, y un aviso cuando puede haber cambiado (F-24).
/// </summary>
/// <remarks>
/// <para>
/// El aviso sale de <see cref="UISettings.ColorValuesChanged"/> y no de
/// <see cref="AccessibilitySettings.HighContrastChanged"/>: en una app de escritorio (sin <c>CoreWindow</c>),
/// suscribirse a este último lanza <c>0x80070490</c> y tumbaba la aplicación. Lo destapó la prueba en la app real;
/// los tests unitarios no pueden verlo.
/// </para>
/// <para>
/// <see cref="Changed"/> salta también con otros cambios de color del sistema (el acento, el modo claro u oscuro):
/// quien lo escuche solo tiene que volver a mirar <see cref="IsActive"/>. Llega desde otro hilo, y los objetos de
/// Windows viven aquí, estáticos, para que la suscripción dure lo que dure la aplicación.
/// </para>
/// </remarks>
internal static class ContrastMode
{
    private static readonly AccessibilitySettings Accessibility = new();
    private static readonly UISettings Ui = new();

    static ContrastMode() => Ui.ColorValuesChanged += (_, _) => Changed?.Invoke();

    /// <summary>Hay un tema de contraste activo.</summary>
    internal static bool IsActive => Accessibility.HighContrast;

    /// <summary>Los colores del sistema cambiaron. Llega desde otro hilo.</summary>
    internal static event Action? Changed;
}
