using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Regresión de snap layouts de Windows 11 contra la app real (ROADMAP.md Tier B #7). Al arrastrar la
/// ventana a una celda de snap, Windows la redimensiona a media pantalla (<c>work.Width/2 x work.Height</c>)
/// o a un cuarto (<c>work.Width/2 x work.Height/2</c>). Dos cosas tenían que arreglarse para que la app
/// sobreviviera a eso, y cada una tiene aquí su test:
///
/// 1. <b>El mínimo de la ventana bloqueaba el snap.</b> <c>OverlappedPresenter.PreferredMinimumWidth/Height</c>
///    (que <c>UI/WindowSizer.cs</c> calcula con <c>Core/WindowSizing.ScaleMinSize</c>) salía de un mínimo de
///    diseño de 900x600 DIP. En un 1920x1080 la celda de cuarto mide 960x520, así que el mínimo de alto
///    (600) ya no cabía y Windows no podía encoger la ventana lo suficiente. <c>ScaleMinSize</c> ahora acota
///    también cada eje a la mitad del área de trabajo, y los asserts de "cabe en la celda" lo comprueban.
///
/// 2. <b>La tabla desaparecía de la pantalla.</b> Con la ventana ya encogida, las tres tarjetas superiores
///    (cabecera, acciones, filtros) consumían los 510px de alto enteros y el DataGrid quedaba recortado
///    fuera de la ventana: no "bajo el pliegue", sino con <c>BoundingRectangle</c> 0x0, sin barra de scroll
///    con la que llegar a él. Ahora todo el contenido vive en el <c>ContentScroller</c> de
///    <c>UI/MainWindow.xaml</c>, así que en la celda de cuarto la página se desplaza y la tabla es
///    alcanzable: eso es justo lo que asegura <see cref="MainWindow_FitsQuarterScreenSnapCell"/>.
///
/// La matemática pura del punto 1 está cubierta aparte en <c>WindowSizingTests.ScaleMinSize_*SnapCell_*</c>.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SnapLayoutTests(AppFixture fixture)
{
    private static readonly string[] ActionButtonIds =
    [
        "btnConsultar", "btnConsultarDesconocidas", "btnActualizarSeleccionados",
        "btnActualizarTodo", "btnCancelar", "btnHerramientas", "btnAyuda"
    ];

    // Sombra de ventana/redondeo de la composición de escritorio: unos pocos px de más no significan
    // que el snap realmente haya fallado a encajar en la celda.
    private const int Tolerance = 8;

    /// <summary>
    /// Celda de media pantalla (mitad del ancho, alto completo del área de trabajo). Sobra alto, así que
    /// el <c>ContentScroller</c> no necesita desplazarse: la tabla tiene que verse directamente, sin
    /// scroll, tal como se ve hoy en una ventana normal.
    /// </summary>
    [Fact]
    public void MainWindow_FitsHalfScreenSnapCell()
    {
        var work = GetWorkAreaUnderMainWindow();
        int cellWidth = (work.Right - work.Left) / 2;
        int cellHeight = work.Bottom - work.Top;

        InSnapCell(cellWidth, cellHeight, () =>
        {
            AssertWindowFitsCell(cellWidth, cellHeight);
            AssertActionButtonsVisible(cellWidth, cellHeight);

            var grid = FindOrFail("lvPackages", cellWidth, cellHeight);
            Assert.False(grid.BoundingRectangle.IsEmpty,
                $"'lvPackages' tiene BoundingRectangle vacío en la celda de media pantalla {cellWidth}x{cellHeight}.");
            Assert.False(grid.IsOffscreen,
                $"'lvPackages' no se ve en la celda de media pantalla {cellWidth}x{cellHeight} pese a que sobra alto.");
        });
    }

    /// <summary>
    /// Celda de cuarto de pantalla: el caso que motivó #7. Aquí el contenido no cabe entero (las tarjetas
    /// superiores solas ya llenan la celda), y eso es aceptable: lo que NO es aceptable es que la tabla
    /// quede irrecuperable. Por eso el test no exige verla nada más encoger -- exige que la página se
    /// pueda desplazar y que, al bajar del todo, la tabla esté ahí y visible. Antes del arreglo del
    /// <c>ContentScroller</c> esto era imposible: no había barra de scroll y el DataGrid tenía tamaño 0x0.
    /// </summary>
    [Fact]
    public void MainWindow_FitsQuarterScreenSnapCell()
    {
        var work = GetWorkAreaUnderMainWindow();
        int cellWidth = (work.Right - work.Left) / 2;
        int cellHeight = (work.Bottom - work.Top) / 2;

        InSnapCell(cellWidth, cellHeight, () =>
        {
            AssertWindowFitsCell(cellWidth, cellHeight);

            // Las acciones rápidas quedan arriba del todo, así que se comprueban antes de desplazar.
            AssertActionButtonsVisible(cellWidth, cellHeight);

            var scroller = FindOrFail("ContentScroller", cellWidth, cellHeight);
            var scrollPattern = scroller.Patterns.Scroll;
            Assert.True(scrollPattern.IsSupported,
                "ContentScroller no expone ScrollPattern: la página no puede desplazarse.");

            var scroll = scrollPattern.Pattern;
            Assert.True(scroll.VerticallyScrollable.Value,
                $"La página no es desplazable en la celda de cuarto {cellWidth}x{cellHeight}: el contenido " +
                "que no cabe (tabla, log, barra de estado) queda irrecuperable para el usuario.");

            // -1 (NoScroll) en horizontal: ContentScroller tiene el scroll horizontal deshabilitado.
            scroll.SetScrollPercent(-1, 100);
            Thread.Sleep(500);

            var grid = FindOrFail("lvPackages", cellWidth, cellHeight);
            Assert.False(grid.BoundingRectangle.IsEmpty,
                $"Tras desplazar al final en la celda de cuarto {cellWidth}x{cellHeight}, 'lvPackages' sigue con BoundingRectangle vacío.");
            Assert.False(grid.IsOffscreen,
                $"Tras desplazar al final en la celda de cuarto {cellWidth}x{cellHeight}, 'lvPackages' sigue fuera de pantalla.");
        });
    }

    private MonitorInfoHelper.RECT GetWorkAreaUnderMainWindow()
    {
        var bounds = fixture.MainWindow.BoundingRectangle;
        return MonitorInfoHelper.GetWorkArea(new MonitorInfoHelper.RECT
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Right = bounds.Right,
            Bottom = bounds.Bottom
        });
    }

    /// <summary>
    /// Encoge la ventana al tamaño de la celda de snap, ejecuta los asserts y la deja como estaba: la
    /// colección comparte una única instancia de <see cref="AppFixture"/> entre todos los tests.
    /// </summary>
    private void InSnapCell(int cellWidth, int cellHeight, Action asserts)
    {
        var transformPattern = fixture.MainWindow.Patterns.Transform;
        Assert.True(transformPattern.IsSupported, "MainWindow no soporta TransformPattern (no se puede redimensionar por UIA).");

        var transform = transformPattern.Pattern;
        Assert.True(transform.CanResize.Value, "MainWindow no admite CanResize.");

        var originalBounds = fixture.MainWindow.BoundingRectangle;
        try
        {
            // Coordenadas en píxeles físicos: el proceso de test ya es PerMonitorV2 DPI-aware (ver
            // DpiAwareness en LayoutTests.cs), igual que la app.
            transform.Resize(cellWidth, cellHeight);

            Retry.WhileTrue(
                () => fixture.MainWindow.BoundingRectangle.Width == originalBounds.Width
                    && fixture.MainWindow.BoundingRectangle.Height == originalBounds.Height,
                timeout: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);

            // Igual que en LayoutTests.ActionButtons_RemainVisible_WhenWindowIsNarrow: el WrapPanel
            // necesita un pase de Measure/Arrange propio tras el resize antes de que el árbol de
            // automatización refleje el reflow.
            Thread.Sleep(500);

            asserts();
        }
        finally
        {
            transform.Resize(Math.Max(originalBounds.Width, 1180), Math.Max(originalBounds.Height, 820));
            Retry.WhileTrue(
                () => fixture.MainWindow.BoundingRectangle.Width < 900,
                timeout: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);
        }
    }

    private void AssertWindowFitsCell(int cellWidth, int cellHeight)
    {
        var bounds = fixture.MainWindow.BoundingRectangle;

        Assert.True(bounds.Width <= cellWidth + Tolerance,
            $"MainWindow.Width={bounds.Width} no cabe en la celda de snap (ancho de celda={cellWidth}); el mínimo de la ventana está bloqueando el snap.");
        Assert.True(bounds.Height <= cellHeight + Tolerance,
            $"MainWindow.Height={bounds.Height} no cabe en la celda de snap (alto de celda={cellHeight}); el mínimo de la ventana está bloqueando el snap.");
    }

    private void AssertActionButtonsVisible(int cellWidth, int cellHeight)
    {
        foreach (var id in ActionButtonIds)
        {
            var button = FindOrFail(id, cellWidth, cellHeight);
            Assert.False(button.BoundingRectangle.IsEmpty,
                $"El botón '{id}' tiene BoundingRectangle vacío en la celda de snap {cellWidth}x{cellHeight} (recortado/oculto).");
            Assert.False(button.IsOffscreen,
                $"El botón '{id}' quedó fuera de pantalla en la celda de snap {cellWidth}x{cellHeight}.");
        }
    }

    private AutomationElement FindOrFail(string automationId, int cellWidth, int cellHeight)
    {
        var result = Retry.WhileNull(
            () => fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout: TimeSpan.FromSeconds(5),
            interval: TimeSpan.FromMilliseconds(250),
            ignoreException: true);

        Assert.True(result.Success && result.Result is not null,
            $"No se encontró '{automationId}' en la celda de snap {cellWidth}x{cellHeight}.");

        return result.Result!;
    }
}
