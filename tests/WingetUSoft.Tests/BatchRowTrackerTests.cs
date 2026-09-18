using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Estado de cada fila durante un lote de actualizaciones (F-19).
/// </summary>
/// <remarks>
/// Hasta F-19 la tabla se deshabilitaba entera mientras duraba el lote y el avance solo se veía en la barra de estado.
/// Estos tests recorren las transiciones —en cola, en curso, correcto, fallido— con el lote de verdad
/// (<see cref="UpgradeBatchRunner"/>) y winget sustituido por un doble: sin ventana ni procesos.
/// </remarks>
public sealed class BatchRowTrackerTests
{
    /// <summary>Winget de mentira: responde lo que se le diga y avisa antes de cada paquete.</summary>
    private sealed class FakeWinget(Func<string, UpgradeResult> responder) : IWingetService
    {
        public Action<string>? BeforeEachCall { get; init; }

        public Task<UpgradeResult> UpgradePackageAsync(
            string packageId,
            bool silent,
            IProgress<WingetProgressInfo>? progress,
            CancellationToken cancellationToken,
            IProgress<string>? logProgress)
        {
            BeforeEachCall?.Invoke(packageId);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(packageId));
        }
    }

    private static WingetPackage Package(string id) =>
        new() { Name = id + " App", Id = id, Version = "1.0", Available = "2.0", Source = "winget" };

    private static UpgradeResult Ok() => new() { Success = true, ExitCode = 0 };

    private static UpgradeResult Fails(string errorOutput) =>
        new() { Success = false, ExitCode = 1, ErrorOutput = errorOutput };

    [Fact]
    public void Begin_QueuesEveryPackageOfTheBatch()
    {
        var tracker = new BatchRowTracker();

        tracker.Begin([Package("A"), Package("B")]);

        Assert.Equal(RowOperationState.Queued, tracker.Get("A").State);
        Assert.Equal(RowOperationState.Queued, tracker.Get("B").State);
        Assert.Equal(RowOperationState.None, tracker.Get("Z").State);
    }

    [Fact]
    public void Ids_AreMatchedIgnoringCase_LikeWingetDoes()
    {
        var tracker = new BatchRowTracker();
        tracker.Begin([Package("Git.Git")]);

        Assert.Equal(RowOperationState.Queued, tracker.Get("git.git").State);
    }

    /// <summary>El lote real lleva cada fila por sus estados, y el fallo se queda con su motivo.</summary>
    [Fact]
    public async Task RealBatch_TakesEachRowThroughItsStates()
    {
        var tracker = new BatchRowTracker();
        var packages = new[] { Package("A"), Package("B"), Package("C") };
        var seenWhileRunning = new List<(string Id, RowOperationState A, RowOperationState B, RowOperationState C)>();

        var winget = new FakeWinget(id => id == "B" ? Fails("0x8A15002B") : Ok())
        {
            // Justo cuando winget recibe cada paquete: ese va en curso y los siguientes, en cola.
            BeforeEachCall = id => seenWhileRunning.Add((id, tracker.Get("A").State, tracker.Get("B").State, tracker.Get("C").State)),
        };

        tracker.Begin(packages);
        await UpgradeBatchRunner.RunAsync(winget, packages, silent: true, tracker, CancellationToken.None);
        tracker.Finish();

        Assert.Equal(("A", RowOperationState.Running, RowOperationState.Queued, RowOperationState.Queued), seenWhileRunning[0]);
        Assert.Equal(("B", RowOperationState.Succeeded, RowOperationState.Running, RowOperationState.Queued), seenWhileRunning[1]);
        Assert.Equal(("C", RowOperationState.Succeeded, RowOperationState.Failed, RowOperationState.Running), seenWhileRunning[2]);

        Assert.Equal(RowOperationState.Succeeded, tracker.Get("A").State);
        Assert.Equal(RowOperationState.Failed, tracker.Get("B").State);
        Assert.False(string.IsNullOrWhiteSpace(tracker.Get("B").Reason));
        Assert.Equal(RowOperationState.Succeeded, tracker.Get("C").State);
    }

    /// <summary>Al cancelar, lo que no llegó a empezar ni a acabar deja de decir que se va a actualizar.</summary>
    [Fact]
    public async Task CancelledBatch_ClearsWhatNeverFinished()
    {
        var tracker = new BatchRowTracker();
        var packages = new[] { Package("A"), Package("B"), Package("C") };
        using var cts = new CancellationTokenSource();

        var winget = new FakeWinget(_ => Ok())
        {
            BeforeEachCall = id => { if (id == "B") cts.Cancel(); },
        };

        tracker.Begin(packages);
        await UpgradeBatchRunner.RunAsync(winget, packages, silent: true, tracker, cts.Token);
        tracker.Finish();

        Assert.Equal(RowOperationState.Succeeded, tracker.Get("A").State);
        Assert.Equal(RowOperationState.None, tracker.Get("B").State);   // estaba en curso cuando se canceló
        Assert.Equal(RowOperationState.None, tracker.Get("C").State);   // no llegó a empezar
    }

    /// <summary>Tras la recarga que sigue al lote solo quedan marcados los fallos, que es lo que se querrá reintentar.</summary>
    [Fact]
    public void KeepOnlyFailures_DropsEverythingElse()
    {
        var tracker = new BatchRowTracker();
        tracker.Begin([Package("A"), Package("B")]);
        tracker.MarkSucceeded("A");
        tracker.MarkFailed("B", "sin conexión");

        tracker.KeepOnlyFailures();

        Assert.Equal(RowOperationState.None, tracker.Get("A").State);
        Assert.Equal(new RowOperation(RowOperationState.Failed, "sin conexión"), tracker.Get("B"));
    }

    /// <summary>Un lote nuevo no arrastra las marcas del anterior.</summary>
    [Fact]
    public void Begin_DiscardsThePreviousBatch()
    {
        var tracker = new BatchRowTracker();
        tracker.Begin([Package("A")]);
        tracker.MarkFailed("A", "error");

        tracker.Begin([Package("B")]);

        Assert.Equal(RowOperationState.None, tracker.Get("A").State);
        Assert.Equal(RowOperationState.Queued, tracker.Get("B").State);
    }

    /// <summary>Cada cambio se avisa, también el de volver a no tener estado: es lo que repinta la fila.</summary>
    [Fact]
    public void EveryChange_IsAnnounced()
    {
        var tracker = new BatchRowTracker();
        var changes = new List<(string Id, RowOperationState State)>();
        tracker.Changed += (id, op) => changes.Add((id, op.State));

        tracker.Begin([Package("A")]);
        tracker.MarkRunning("A");
        tracker.Finish();

        Assert.Equal(
            [("A", RowOperationState.Queued), ("A", RowOperationState.Running), ("A", RowOperationState.None)],
            changes);
    }

    /// <summary>
    /// La tabla ya no se deshabilita durante una operación: queda en modo lectura. Deshabilitada no se podía ni
    /// recorrer, y los estados por fila no servirían de nada.
    /// </summary>
    [Fact]
    public void MainWindow_NoLongerDisablesTheTable()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WingetUSoft.slnx")))
            dir = dir.Parent;
        Assert.True(dir is not null, "No se encontró la raíz del repositorio (WingetUSoft.slnx).");

        string ui = Path.Combine(dir!.FullName, "src", "WingetUSoft", "UI");
        foreach (string file in Directory.EnumerateFiles(ui, "MainWindow*.cs"))
            Assert.DoesNotContain("lvPackages.IsEnabled", File.ReadAllText(file), StringComparison.Ordinal);
    }

    /// <summary>La fila anuncia su estado —con el motivo si falló— y se bloquea en modo lectura.</summary>
    [Fact]
    public void Row_AnnouncesItsState_AndLocksWhileBusy()
    {
        var row = new PackageViewModel(Package("A"));
        var original = L.Current;
        try
        {
            L.Set(AppLang.Es);

            row.Operation = new RowOperation(RowOperationState.Failed, "sin conexión");
            Assert.EndsWith("Falló: sin conexión", row.RowLabel, StringComparison.Ordinal);
            Assert.Equal("Falló: sin conexión", row.StatusLabel);
            Assert.True(row.ShowStatusGlyph);

            row.Operation = new RowOperation(RowOperationState.Running);
            Assert.True(row.IsOperationRunning);
            Assert.False(row.ShowStatusGlyph);   // en curso lleva anillo, no glifo

            Assert.True(row.IsSelectable);
            row.IsLocked = true;
            Assert.False(row.IsSelectable);
        }
        finally { L.Set(original); }
    }
}
