using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WingetUSoft;

/// <summary>
/// Diálogos compartidos por <c>MainWindow</c>, <c>SearchWindow</c>, <c>UninstallWindow</c> y
/// <c>CleanupWindow</c>, es decir, prácticamente todos los de la aplicación.
/// </summary>
/// <remarks>
/// Los textos de los botones se resuelven con <see cref="L.T"/> **en cada llamada**, nunca como literal:
/// hasta la auditoría del 2026-08-20 «Aceptar», «Sí» y «No» estaban cableados en español y se mostraban tal
/// cual en los cinco idiomas.
/// </remarks>
internal static class WindowDialogHelper
{
    /// <summary>
    /// Único punto donde se prepara un diálogo para mostrarse: lo ancla a la ventana de
    /// <paramref name="xamlRoot"/> y le da el tema de esa ventana.
    /// </summary>
    /// <remarks>
    /// El tema se fuerza por elemento (<c>RequestedTheme</c> en la raíz de cada ventana) y un
    /// <c>ContentDialog</c> no lo hereda, porque se dibuja en la capa de popups y no bajo esa raíz. Hasta
    /// F-03 solo tres diálogos lo copiaban a mano; los de este helper —todas las confirmaciones y los
    /// errores— salían con el tema del sistema: con la app en Claro sobre Windows oscuro se pintaban
    /// oscuros. <c>DialogPreparationTests</c> impide volver a crear un diálogo sin pasar por aquí.
    /// </remarks>
    internal static T Prepare<T>(T dialog, XamlRoot xamlRoot) where T : ContentDialog
    {
        dialog.XamlRoot = xamlRoot;
        if (xamlRoot.Content is FrameworkElement root)
            dialog.RequestedTheme = root.RequestedTheme;
        return dialog;
    }

    internal static async Task ShowDialogAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = Prepare(new ContentDialog
        {
            Title           = title,
            Content         = message,
            CloseButtonText = L.T("btn.accept"),
        }, xamlRoot);
        await dialog.ShowAsync();
    }

    /// <summary>Confirmación de una acción; devuelve <c>true</c> si el usuario la confirma.</summary>
    /// <param name="primaryText">
    /// El verbo de la acción («Desinstalar», «Actualizar», «Importar e instalar»…). Es obligatorio: hasta F-04
    /// todas las confirmaciones respondían «Sí / No», y la guía de diálogos de Windows pide que cada botón diga
    /// la respuesta concreta, que se entiende sin volver a leer la pregunta.
    /// </param>
    /// <param name="destructive">
    /// La acción no se puede deshacer (desinstalar, borrar residuos). Ver <see cref="DefaultButtonFor"/>.
    /// </param>
    internal static async Task<bool> ShowConfirmDialogAsync(
        XamlRoot xamlRoot,
        string title,
        string message,
        string primaryText,
        bool destructive = false)
    {
        var dialog = Prepare(new ContentDialog
        {
            Title             = title,
            Content           = message,
            PrimaryButtonText = primaryText,
            CloseButtonText   = L.T("btn.cancel"),
            DefaultButton     = DefaultButtonFor(destructive),
        }, xamlRoot);
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Botón por defecto de una confirmación: el que recibe el foco al abrir y responde a Intro.
    /// </summary>
    /// <remarks>
    /// En una acción destructiva es <b>Cancelar</b>. Hasta F-04 el primario era siempre el botón por defecto,
    /// así que un Intro de más —o un teclado que ya tenía el foco— desinstalaba un programa o borraba carpetas
    /// de forma recursiva. Dejar el diálogo sin botón por defecto no bastaría: el foco inicial caería igualmente
    /// en el primario.
    /// </remarks>
    internal static ContentDialogButton DefaultButtonFor(bool destructive) =>
        destructive ? ContentDialogButton.Close : ContentDialogButton.Primary;
}
