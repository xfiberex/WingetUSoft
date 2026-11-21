using System.Xml.Linq;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Desinstalación en lote, con un doble en lugar de winget: ningún test desinstala nada del equipo.
/// </summary>
public sealed class UninstallBatchTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static List<WingetPackage> Packages(params string[] ids) =>
        ids.Select(id => new WingetPackage { Id = id, Name = id }).ToList();

    private static Task<UpgradeResult> Ok() => Task.FromResult(new UpgradeResult { Success = true });

    private static Task<UpgradeResult> Fail() =>
        Task.FromResult(new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = "fallo de prueba" });

    [Fact]
    public async Task AllSucceed_InOrder()
    {
        var order = new List<string>();
        var result = await UninstallBatch.RunAsync(Packages("a", "b", "c"), (p, _) =>
        {
            order.Add(p.Id);
            return Ok();
        });

        Assert.Equal(new[] { "a", "b", "c" }, order);
        Assert.Equal(3, result.Succeeded.Count);
        Assert.Empty(result.Failed);
        Assert.False(result.Cancelled);
    }

    /// <summary>Un programa que falla no impide desinstalar los siguientes.</summary>
    [Fact]
    public async Task AFailure_DoesNotStopTheBatch()
    {
        var result = await UninstallBatch.RunAsync(Packages("a", "b", "c"),
            (p, _) => p.Id == "b" ? Fail() : Ok());

        Assert.Equal(new[] { "a", "c" }, result.Succeeded.Select(p => p.Id));
        Assert.Equal("b", Assert.Single(result.Failed).Package.Id);
    }

    [Fact]
    public async Task AnException_CountsAsAFailure_AndTheBatchContinues()
    {
        var result = await UninstallBatch.RunAsync(Packages("a", "b"), (p, _) =>
            p.Id == "a" ? throw new InvalidOperationException("sin winget") : Ok());

        Assert.Equal("sin winget", Assert.Single(result.Failed).Reason);
        Assert.Equal("b", Assert.Single(result.Succeeded).Id);
    }

    /// <summary>
    /// Cancelar en un programa no intenta los siguientes, y el que estaba en curso cuenta como sin terminar:
    /// no consta que se desinstalara, así que no se le ofrece limpieza.
    /// </summary>
    [Fact]
    public async Task Cancelling_StopsTheBatch_AndKeepsWhatFinished()
    {
        using var cts = new CancellationTokenSource();
        var attempted = new List<string>();

        var result = await UninstallBatch.RunAsync(Packages("a", "b", "c"), (p, ct) =>
        {
            attempted.Add(p.Id);
            if (p.Id == "b")
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
            }
            return Ok();
        }, cancellationToken: cts.Token);

        Assert.Equal(new[] { "a", "b" }, attempted);
        Assert.True(result.Cancelled);
        Assert.Equal("a", Assert.Single(result.Succeeded).Id);
        Assert.Equal(2, result.NotCompleted);
    }

    [Fact]
    public async Task AlreadyCancelled_AttemptsNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        int calls = 0;

        var result = await UninstallBatch.RunAsync(Packages("a", "b"), (_, _) =>
        {
            calls++;
            return Ok();
        }, cancellationToken: cts.Token);

        Assert.Equal(0, calls);
        Assert.Equal(2, result.NotCompleted);
    }

    [Fact]
    public async Task Callbacks_ReportPositionAndOutcome()
    {
        var starts = new List<(string, int)>();
        var ok = new List<string>();
        var ko = new List<string>();

        await UninstallBatch.RunAsync(Packages("a", "b"),
            (p, _) => p.Id == "a" ? Ok() : Fail(),
            onStarting: (p, position) => starts.Add((p.Id, position)),
            onSucceeded: p => ok.Add(p.Id),
            onFailed: f => ko.Add(f.Package.Id));

        Assert.Equal(new[] { ("a", 1), ("b", 2) }, starts);
        Assert.Equal(new[] { "a" }, ok);
        Assert.Equal(new[] { "b" }, ko);
    }

    /// <summary>
    /// Los avisos tocan controles: el bucle tiene que volver al hilo de la interfaz. Es lo que rompió los lotes de
    /// actualización de la 1.8.8 a la 1.10.0 (F-19).
    /// </summary>
    [Fact]
    public void TheLoop_DoesNotLeaveTheCallingContext()
    {
        string code = File.ReadAllText(Path.Combine(SourceDirectory(), "Core", "UninstallBatch.cs"));
        Assert.DoesNotContain(".ConfigureAwait(false)", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>--exact</c> es obligatorio: sin él, <c>--id Microsoft.Edge</c> coincide también con
    /// <c>Microsoft.Edge.GameAssist</c>, y desinstalar tiene que señalar un solo programa.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UninstallArguments_MatchTheIdExactly(bool silent)
    {
        var arguments = WingetService.BuildUninstallArguments("Microsoft.Edge", silent);

        Assert.Equal("Microsoft.Edge", arguments[arguments.IndexOf("--id") + 1]);
        Assert.Contains("--exact", arguments);
        Assert.Equal(silent, arguments.Contains("--silent"));
    }

    /// <summary>Cabecera y filas siguen teniendo las mismas columnas, y cada casilla se anuncia con su programa.</summary>
    [Fact]
    public void UninstallTable_HasANamedCheckBoxColumn_AlignedWithItsHeader()
    {
        var doc = XDocument.Load(Path.Combine(SourceDirectory(), "UI", "UninstallWindow.xaml"));

        var widths = doc.Descendants(Presentation + "Grid.ColumnDefinitions")
            .Select(c => string.Join(",", c.Elements().Select(d => (string?)d.Attribute("Width"))))
            .Where(w => w.StartsWith("44,", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, widths.Count);
        Assert.Equal(widths[0], widths[1]);

        var check = doc.Descendants(Presentation + "CheckBox")
            .Single(c => (string?)c.Attribute("IsChecked") == "{Binding IsMarked, Mode=TwoWay}");
        Assert.Equal("{Binding SelectLabel}", (string?)check.Attribute("AutomationProperties.Name"));
    }

    private static string SourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx).");
        return Path.Combine(dir!.FullName, "src", "WingetUSoft");
    }
}
