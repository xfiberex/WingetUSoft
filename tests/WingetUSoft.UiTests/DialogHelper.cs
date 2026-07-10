using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Ayudas para interactuar con los <c>ContentDialog</c> de WinUI. Igual que en FormatDiskPro: esta app
/// NO abre ningún HWND top-level nuevo para un ContentDialog (solo existe la ventana "WingetUSoft" a
/// nivel de Desktop) -- el ContentDialog vive como descendiente de MainWindow. El árbol de MainWindow
/// puede contener MÁS DE UN elemento ControlType.Window a la vez (WinUI deja un proxy de Popup vacío,
/// "Ventana emergente", además del diálogo real); por eso <c>FindFirstDescendant(ByControlType.Window)</c>
/// puede atrapar el proxy vacío en vez del diálogo. Se buscan TODOS los candidatos y se toma el que de
/// verdad tiene contenido. Los botones nativos (Primary/Secondary/Close) heredan su AutomationId del
/// x:Name que WinUI les da en su plantilla, igual que los controles nombrados en el XAML de la app.
/// </summary>
public static class DialogHelper
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    private static Window? FindDialog(AppFixture fixture)
    {
        var candidates = fixture.MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Window));
        if (candidates.Length == 0) return null;

        // Todo diálogo real de esta app fija siempre CloseButtonText y/o Primary/SecondaryButtonText,
        // así que WinUI le da chrome con al menos uno de esos tres AutomationId. El proxy de Popup vacío
        // ("Ventana emergente") y los residuos de un MenuFlyout que aún no ha terminado de cerrarse NO
        // tienen ninguno de esos botones.
        return candidates
            .Select(w => w.AsWindow())
            .FirstOrDefault(HasDialogChrome);
    }

    private static bool HasDialogChrome(AutomationElement w)
    {
        try
        {
            return w.FindFirstDescendant(cf => cf.ByAutomationId("PrimaryButton")) is not null
                || w.FindFirstDescendant(cf => cf.ByAutomationId("SecondaryButton")) is not null
                || w.FindFirstDescendant(cf => cf.ByAutomationId("CloseButton")) is not null;
        }
        catch { return false; }
    }

    public static Window WaitForDialog(AppFixture fixture, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => FindDialog(fixture),
            timeout: timeout ?? DefaultTimeout,
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        if (result.Result is not null) return result.Result;

        throw new InvalidOperationException(
            "No se abrió ningún ContentDialog (ControlType.Window descendiente de MainWindow) dentro " +
            $"del tiempo esperado.\n{DumpWindowCandidates(fixture)}");
    }

    /// <summary>
    /// Como <see cref="WaitForDialog"/>, pero exige que el candidato contenga un descendiente con el
    /// AutomationId dado. Útil cuando puede haber MÁS de un ControlType.Window compitiendo a la vez
    /// (p. ej. un MenuFlyout que no ha terminado de cerrarse tras invocar un ítem por patrón UIA en vez
    /// de un clic real).
    /// </summary>
    public static Window WaitForDialogContaining(AppFixture fixture, string automationId, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => fixture.MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.Window))
                .Select(w => w.AsWindow())
                .FirstOrDefault(w =>
                {
                    try { return w.FindFirstDescendant(cf => cf.ByAutomationId(automationId)) is not null; }
                    catch { return false; }
                }),
            timeout: timeout ?? DefaultTimeout,
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        return result.Result ?? throw new InvalidOperationException(
            $"No se encontró ningún diálogo que contenga '{automationId}' dentro del tiempo esperado.\n" +
            DumpWindowCandidates(fixture));
    }

    /// <summary>Diagnóstico: todos los ControlType.Window descendientes de MainWindow y cuántos hijos tiene cada uno.</summary>
    private static string DumpWindowCandidates(AppFixture fixture)
    {
        AutomationElement[] candidates;
        try { candidates = fixture.MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Window)); }
        catch (Exception ex) { return $"(no se pudo enumerar: {ex.Message})"; }

        if (candidates.Length == 0) return "(ningún ControlType.Window encontrado en el árbol de MainWindow)";

        var sb = new System.Text.StringBuilder();
        foreach (var w in candidates)
        {
            int childCount;
            try { childCount = w.FindAllChildren().Length; } catch { childCount = -1; }
            sb.Append($"- Name='{SafeName(w)}' hijos={childCount}\n");
        }
        return sb.ToString();
    }

    public static void WaitForNoDialog(AppFixture fixture, TimeSpan? timeout = null)
    {
        var result = Retry.WhileTrue(
            () => FindDialog(fixture) is not null,
            timeout: timeout ?? DefaultTimeout,
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        if (!result.Success)
            throw new InvalidOperationException("El diálogo abierto no se cerró dentro del tiempo esperado.");
    }

    public static Button PrimaryButton(Window dialog) => FindButton(dialog, "PrimaryButton");
    public static Button SecondaryButton(Window dialog) => FindButton(dialog, "SecondaryButton");
    public static Button CloseButton(Window dialog) => FindButton(dialog, "CloseButton");

    public static Button? TryFindButton(Window dialog, string automationId) =>
        dialog.FindFirstDescendant(cf => cf.ByAutomationId(automationId))?.AsButton();

    private static Button FindButton(Window dialog, string automationId) =>
        TryFindButton(dialog, automationId)
            ?? throw new InvalidOperationException(
                $"No se encontró el botón '{automationId}' en el diálogo abierto.");

    /// <summary>
    /// Reintenta buscar un descendiente por AutomationId dentro de un elemento (normalmente un diálogo
    /// recién detectado): su contenido puede tardar un poco más en poblarse en el árbol de automatización
    /// que el propio elemento ventana que <see cref="WaitForDialog"/> ya detectó.
    /// </summary>
    public static AutomationElement WaitForChild(AutomationElement root, string automationId, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => root.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout: timeout ?? TimeSpan.FromSeconds(10),
            interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true);

        if (result.Result is not null) return result.Result;

        throw new InvalidOperationException(
            $"No se encontró '{automationId}' dentro del elemento (Name='{SafeName(root)}', " +
            $"ControlType={SafeControlType(root)}). Contenido real encontrado:\n{DumpTree(root, 2)}");
    }

    private static string DumpTree(AutomationElement element, int maxDepth, int depth = 0)
    {
        if (depth > maxDepth) return "";
        var sb = new System.Text.StringBuilder();
        AutomationElement[] children;
        try { children = element.FindAllChildren(); }
        catch { return new string(' ', depth * 2) + "(no se pudieron listar los hijos)\n"; }

        foreach (var child in children)
        {
            sb.Append(new string(' ', depth * 2));
            sb.Append($"- AutomationId='{SafeAutomationId(child)}' Name='{SafeName(child)}' ControlType={SafeControlType(child)}\n");
            sb.Append(DumpTree(child, maxDepth, depth + 1));
        }
        return sb.ToString();
    }

    private static string SafeName(AutomationElement element) { try { return element.Name; } catch { return "?"; } }
    private static string SafeAutomationId(AutomationElement element) { try { return element.AutomationId; } catch { return "?"; } }
    private static string SafeControlType(AutomationElement element) { try { return element.ControlType.ToString(); } catch { return "?"; } }

    /// <summary>
    /// Mejor esfuerzo: cierra cualquier ContentDialog que esté abierto en este momento, sin lanzar si ya
    /// no hay ninguno o si el cierre falla. Pensado para bloques <c>finally</c>: WinUI solo permite un
    /// ContentDialog abierto a la vez, y un segundo intento de abrir otro lanza dentro de un manejador
    /// <c>async void</c> sin captura -- puede tirar abajo el proceso entero. Sin esto, un assert fallido
    /// a media prueba deja el diálogo abierto y revienta TODA la suite restante.
    /// </summary>
    public static void SafeCloseAnyDialog(AppFixture fixture, TimeSpan? timeout = null)
    {
        try
        {
            var dialog = FindDialog(fixture);
            if (dialog is null) return;

            var closeLike = TryFindButton(dialog, "CloseButton")
                ?? TryFindButton(dialog, "PrimaryButton")
                ?? TryFindButton(dialog, "SecondaryButton");

            if (closeLike is not null)
            {
                closeLike.Invoke();
            }
            else
            {
                // Sin botón localizable por AutomationId: Escape es la vía universal de WinUI para
                // cerrar un ContentDialog (equivale a su CloseButton), sin depender de encontrarlo.
                try { dialog.Focus(); } catch { }
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
            }

            WaitForNoDialog(fixture, timeout ?? TimeSpan.FromSeconds(10));
        }
        catch { /* mejor esfuerzo: no tapar el fallo real del test con un error de limpieza */ }
    }

    /// <summary>
    /// Vigila y descarta, durante una ventana de tiempo, cualquier diálogo que la propia app abra por su
    /// cuenta (Novedades / Actualización disponible, que <c>MainWindow</c> puede disparar en su Loaded
    /// de arranque).
    /// </summary>
    public static void DismissStartupDialogs(AppFixture fixture, TimeSpan window)
    {
        var deadline = DateTime.UtcNow + window;
        while (DateTime.UtcNow < deadline)
        {
            if (FindDialog(fixture) is not null)
                SafeCloseAnyDialog(fixture, TimeSpan.FromSeconds(10));
            Thread.Sleep(300);
        }
    }

    /// <summary>
    /// Lee el texto visible de un TextBlock (ControlType.Text): el patrón Text de UIA (pensado justo
    /// para exponer contenido de solo lectura, potencialmente largo) es más fiable que la propiedad
    /// Name -- que para bloques de texto extensos puede venir vacía aunque el elemento se localice bien
    /// por AutomationId.
    /// </summary>
    public static string ReadText(AutomationElement element)
    {
        if (element.Patterns.Text.IsSupported)
        {
            try { return element.Patterns.Text.Pattern.DocumentRange.GetText(-1); }
            catch { /* cae a Name */ }
        }
        return element.Name;
    }
}
