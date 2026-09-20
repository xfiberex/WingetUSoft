using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace WingetUSoft.UiTests;

/// <summary>
/// Más espacio para los datos, medido en la app real (F-17).
/// </summary>
/// <remarks>
/// A 1180×820 las cabeceras de columna empezaban al 59 % del alto de la ventana y la tabla enseñaba tres filas. No se
/// consulta nada: el sitio que queda para la tabla no depende de que haya resultados.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class DensityUiTests(AppFixture fixture)
{
    [Fact]
    public void ColumnHeaders_StartAboveThirtyPercentOfTheWindow()
    {
        InSize(1180, 820, () =>
        {
            var window = fixture.MainWindow.BoundingRectangle;
            var header = Find("btnColNombre").BoundingRectangle;

            double share = (header.Top - window.Top) / (double)window.Height;
            Assert.True(share < 0.30, $"Las cabeceras de columna empiezan al {share:P0} del alto (criterio: por encima del 30 %).");
        });
    }

    /// <summary>
    /// El registro arranca plegado y se despliega a mano. Desplegado, su contenido se ve y la tabla conserva sitio;
    /// al plegarlo, la tabla recupera el alto.
    /// </summary>
    [Fact]
    public void ActivityLog_StartsCollapsed_AndExpandsOnDemand()
    {
        var expander = Find("expLog");
        var pattern = expander.Patterns.ExpandCollapse.Pattern;
        Assert.Equal(ExpandCollapseState.Collapsed, pattern.ExpandCollapseState.Value);

        double collapsedTable = Find("lvPackages").BoundingRectangle.Height;
        try
        {
            pattern.Expand();
            Retry.WhileFalse(() => pattern.ExpandCollapseState.Value == ExpandCollapseState.Expanded,
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), ignoreException: true);
            Thread.Sleep(600);   // la animación de despliegue

            double expandedTable = Find("lvPackages").BoundingRectangle.Height;
            Assert.True(expandedTable < collapsedTable,
                $"Al desplegar el registro, la tabla debería ceder alto ({collapsedTable} → {expandedTable}).");
            Assert.True(expandedTable >= 100, $"Con el registro desplegado, a la tabla le quedan {expandedTable} px.");
        }
        finally
        {
            pattern.Collapse();
            Retry.WhileFalse(() => pattern.ExpandCollapseState.Value == ExpandCollapseState.Collapsed,
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), ignoreException: true);
            Thread.Sleep(600);
        }

        Assert.Equal(collapsedTable, Find("lvPackages").BoundingRectangle.Height, precision: 0);
    }

    /// <summary>Con icono y etiqueta por contenido, los botones siguen anunciando su nombre.</summary>
    [Theory]
    [InlineData("btnConsultar", "Consultar actualizaciones")]
    [InlineData("btnActualizarTodo", "Actualizar todo")]
    [InlineData("btnCancelar", "Cancelar")]
    [InlineData("btnHerramientas", "Herramientas")]
    [InlineData("btnAyuda", "Ayuda")]
    public void MainCommands_KeepTheirAccessibleName(string id, string name) =>
        Assert.Equal(name, Find(id).Name);

    private AutomationElement Find(string id)
    {
        var result = Retry.WhileNull(() => fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(id)),
            timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(250), ignoreException: true);
        Assert.True(result.Result is not null, $"No se encontró '{id}'.");
        return result.Result!;
    }

    private void InSize(int width, int height, Action asserts)
    {
        var transform = fixture.MainWindow.Patterns.Transform.Pattern;
        var original = fixture.MainWindow.BoundingRectangle;
        try
        {
            transform.Resize(width, height);
            Retry.WhileFalse(() => fixture.MainWindow.BoundingRectangle.Width == width,
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), ignoreException: true);
            Thread.Sleep(500);   // un pase de layout tras el cambio de tamaño
            asserts();
        }
        finally
        {
            transform.Resize(Math.Max(original.Width, 1180), Math.Max(original.Height, 820));
            Thread.Sleep(300);
        }
    }
}
