using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Alinea la DPI-awareness de ESTE proceso de test con la de la app (PerMonitorV2, ver
/// <c>src/WingetUSoft/app.manifest</c>). Sin esto, un proceso de test no declarado DPI-aware ve las
/// consultas Win32 de monitor (GetMonitorInfo) virtualizadas/escaladas a 96 DPI por Windows, mientras
/// que el <c>BoundingRectangle</c> que reporta UI Automation para la ventana de la app viene en
/// píxeles físicos reales -- comparar ambos sin esto produce falsos positivos/negativos según el
/// escalado del monitor de turno.
/// </summary>
internal static class DpiAwareness
{
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [ModuleInitializer]
    internal static void Initialize() => SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
}

/// <summary>
/// Regresión de layout para Tier B (ver ROADMAP.md #1-#3): la app no se adaptaba a todos los tipos de
/// pantalla (botón "Cancelar" recortado contra el borde de la ventana). Estos tests cubren, contra la
/// app real, los dos arreglos que motivaron la tier: dimensionado por DPI/WorkArea (#1, <c>Core/
/// WindowSizing.cs</c> + <c>UI/WindowSizer.cs</c>) y el WrapPanel responsivo de "Acciones rápidas"
/// (#3, <c>UI/WrapPanel.cs</c> + <c>UI/MainWindow.xaml:96</c>).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class LayoutTests(AppFixture fixture)
{
    private static readonly string[] ActionButtonIds =
    [
        "btnConsultar", "btnConsultarDesconocidas", "btnActualizarSeleccionados",
        "btnActualizarTodo", "btnCancelar", "btnOpciones", "btnAyuda"
    ];

    /// <summary>
    /// Tier B #1: la ventana nunca debería posicionarse ni dimensionarse fuera del área de trabajo del
    /// monitor en el que aparece (el bug original de DPI/WorkArea).
    /// </summary>
    [Fact]
    public void MainWindow_FitsWithinMonitorWorkArea()
    {
        var bounds = fixture.MainWindow.BoundingRectangle;
        var work = MonitorInfoHelper.GetWorkArea(new MonitorInfoHelper.RECT
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Right = bounds.Right,
            Bottom = bounds.Bottom
        });

        // Pequeño margen: sombra de ventana/redondeo de algunas composiciones de escritorio pueden
        // reportar unos pocos px de más sin que la ventana esté realmente fuera de pantalla.
        const int tolerance = 4;
        Assert.True(bounds.Left >= work.Left - tolerance,
            $"MainWindow.Left={bounds.Left} está a la izquierda de WorkArea.Left={work.Left}.");
        Assert.True(bounds.Top >= work.Top - tolerance,
            $"MainWindow.Top={bounds.Top} está por encima de WorkArea.Top={work.Top}.");
        Assert.True(bounds.Right <= work.Right + tolerance,
            $"MainWindow.Right={bounds.Right} se sale de WorkArea.Right={work.Right}.");
        Assert.True(bounds.Bottom <= work.Bottom + tolerance,
            $"MainWindow.Bottom={bounds.Bottom} se sale de WorkArea.Bottom={work.Bottom}.");
    }

    /// <summary>
    /// Tier B #3: al angostar la ventana, los botones de "Acciones rápidas" deben seguir siendo
    /// visibles -- el WrapPanel los reparte en más filas en vez de recortarlos contra el borde (el bug
    /// original reportado por el usuario, con "Cancelar" recortado).
    /// </summary>
    [Fact]
    public void ActionButtons_RemainVisible_WhenWindowIsNarrow()
    {
        var transformPattern = fixture.MainWindow.Patterns.Transform;
        Assert.True(transformPattern.IsSupported, "MainWindow no soporta TransformPattern (no se puede redimensionar por UIA).");

        var transform = transformPattern.Pattern;
        Assert.True(transform.CanResize.Value, "MainWindow no admite CanResize.");

        var originalBounds = fixture.MainWindow.BoundingRectangle;
        try
        {
            // Se pide un ancho deliberadamente menor que el mínimo real de la ventana (Tier B #2,
            // OverlappedPresenter.PreferredMinimumWidth escalado por DPI en WindowSizer.Apply):
            // Windows clampa la petición al mínimo real, que YA es más angosto que la suma de anchos
            // mínimos de los 7 botones -- fuerza el wrap sin tener que calcular aquí el DPI del monitor.
            transform.Resize(400, Math.Max(originalBounds.Height, 600));

            Retry.WhileTrue(
                () => fixture.MainWindow.BoundingRectangle.Width == originalBounds.Width,
                timeout: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);

            // El cambio de ancho de la ventana ya se confirmó arriba, pero el WrapPanel todavía
            // necesita un pase de Measure/Arrange propio para redistribuir los botones en filas -- sin
            // este respiro, una lectura inmediata del árbol de automatización puede encontrarlo a medio
            // reflow (visto en la práctica: FindFirstDescendant no encontraba un botón que sí estaba
            // presente un instante después).
            Thread.Sleep(500);

            foreach (var id in ActionButtonIds)
            {
                var result = Retry.WhileNull(
                    () => fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(id)),
                    timeout: TimeSpan.FromSeconds(5),
                    interval: TimeSpan.FromMilliseconds(250),
                    ignoreException: true);

                Assert.True(result.Success && result.Result is not null, $"No se encontró el botón '{id}' tras angostar la ventana.");
                var button = result.Result!;
                Assert.False(button.BoundingRectangle.IsEmpty, $"El botón '{id}' tiene BoundingRectangle vacío (recortado/oculto).");
                Assert.False(button.IsOffscreen, $"El botón '{id}' quedó fuera de pantalla al angostar la ventana.");
            }
        }
        finally
        {
            // Deja la ventana en un tamaño razonable para el resto de la suite: la colección comparte
            // una única instancia de AppFixture/MainWindow entre todos los tests.
            transform.Resize(Math.Max(originalBounds.Width, 1180), Math.Max(originalBounds.Height, 820));
            Retry.WhileTrue(
                () => fixture.MainWindow.BoundingRectangle.Width < 900,
                timeout: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);
        }
    }
}
