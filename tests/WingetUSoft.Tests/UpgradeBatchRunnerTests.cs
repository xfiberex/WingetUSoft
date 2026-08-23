using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El bucle que actualiza un lote de paquetes es la lógica central del producto y hasta T4-03 no
/// existía un solo test unitario suyo: vivía dentro de <c>MainWindow.UpdatePackagesAsync</c>, atado a
/// la barra de progreso y al texto de estado, y llamaba a la clase estática <c>WingetService</c>. La
/// única forma de ejercitarlo era arrancar la app con FlaUI y dejar que lanzara procesos de verdad.
/// </summary>
/// <remarks>
/// Estos tests no abren ninguna ventana ni lanzan ningún proceso: sustituyen winget por un doble.
/// </remarks>
public sealed class UpgradeBatchRunnerTests
{
    // ── Dobles ──────────────────────────────────────────────────────────────

    /// <summary>Winget de mentira: responde lo que se le diga y anota a quién se le llamó.</summary>
    private sealed class FakeWinget(Func<string, UpgradeResult> responder) : IWingetService
    {
        public List<string> Calls { get; } = [];
        public List<bool> SilentFlags { get; } = [];
        public Action<string>? BeforeEachCall { get; init; }
        public IReadOnlyList<string>? ProgressLines { get; init; }
        public WingetProgressInfo? ProgressToReport { get; init; }

        public Task<UpgradeResult> UpgradePackageAsync(
            string packageId,
            bool silent,
            IProgress<WingetProgressInfo>? progress,
            CancellationToken cancellationToken,
            IProgress<string>? logProgress)
        {
            BeforeEachCall?.Invoke(packageId);
            cancellationToken.ThrowIfCancellationRequested();

            Calls.Add(packageId);
            SilentFlags.Add(silent);

            if (ProgressToReport is { } info) progress?.Report(info);
            foreach (string line in ProgressLines ?? []) logProgress?.Report(line);

            return Task.FromResult(responder(packageId));
        }
    }

    private sealed class RecordingObserver : IUpgradeBatchObserver
    {
        public List<string> Started { get; } = [];
        public List<string> Succeeded { get; } = [];
        public List<(string Package, string Reason)> Failed { get; } = [];
        public List<string> LogLines { get; } = [];
        public List<int> ProgressIndices { get; } = [];

        public void PackageStarting(int index, int total, WingetPackage package) => Started.Add(package.Id);
        public void PackageSucceeded(WingetPackage package) => Succeeded.Add(package.Id);
        public void PackageFailed(WingetPackage package, string reason) => Failed.Add((package.Id, reason));
        public void LogLine(string line) => LogLines.Add(line);
        public void DownloadProgress(int index, int total, WingetPackage package, WingetProgressInfo info) =>
            ProgressIndices.Add(index);
    }

    /// <summary>
    /// Contexto de sincronización que ejecuta cada callback **en el acto**, en el hilo que lo publica.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hace falta porque <see cref="UpgradeBatchRunner"/> reporta el progreso con
    /// <see cref="Progress{T}"/>, y eso **no** es una llamada directa: captura el
    /// <see cref="SynchronizationContext"/> del hilo que lo construye y publica ahí los callbacks. En la
    /// aplicación ese contexto es la cola de la UI, que es justo lo que se quiere — winget reporta desde
    /// un hilo de fondo y tocar un control desde ahí reventaría.
    /// </para>
    /// <para>
    /// En un test no hay contexto, así que <see cref="Progress{T}"/> despacha al pool de hilos y los
    /// callbacks llegan **después** de que el test haya comprobado sus asertos: sin esto, los dos tests
    /// de progreso fallaban de forma engañosa (llegaba solo el último, o tres líneas de cuatro). No se
    /// cambió el código de producción para acomodarlos: el despacho asíncrono es correcto y necesario;
    /// lo que faltaba era un contexto donde observarlo.
    /// </para>
    /// </remarks>
    private sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
        public override void Send(SendOrPostCallback d, object? state) => d(state);

        /// <summary>Instala el contexto y lo restaura al salir del <c>using</c>.</summary>
        public static IDisposable Install()
        {
            SynchronizationContext? previous = Current;
            SetSynchronizationContext(new InlineSynchronizationContext());
            return new Restore(previous);
        }

        private sealed class Restore(SynchronizationContext? previous) : IDisposable
        {
            public void Dispose() => SetSynchronizationContext(previous);
        }
    }

    private static WingetPackage Package(string id) =>
        new() { Name = id + " App", Id = id, Version = "1.0", Available = "2.0", Source = "winget" };

    private static UpgradeResult Ok() => new() { Success = true, ExitCode = 0 };

    private static UpgradeResult Fails(string errorOutput) =>
        new() { Success = false, ExitCode = 1, ErrorOutput = errorOutput };

    private static Task<UpgradeBatchOutcome> RunAsync(
        FakeWinget winget,
        IReadOnlyList<WingetPackage> packages,
        IUpgradeBatchObserver observer,
        CancellationToken cancellationToken = default,
        bool silent = true) =>
        UpgradeBatchRunner.RunAsync(winget, packages, silent, observer, cancellationToken);

    // ── El camino feliz ─────────────────────────────────────────────────────

    [Fact]
    public async Task EveryPackageSucceeds_AllAreCountedAndNoneIsSkipped()
    {
        var winget = new FakeWinget(_ => Ok());
        var observer = new RecordingObserver();
        var packages = new[] { Package("A"), Package("B"), Package("C") };

        UpgradeBatchOutcome result = await RunAsync(winget, packages, observer);

        Assert.Equal(3, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.False(result.Cancelled);
        Assert.Empty(result.Failures);
        // El orden importa: el usuario ve el progreso paquete a paquete en el que pidió.
        Assert.Equal(["A", "B", "C"], winget.Calls);
        Assert.Equal(["A", "B", "C"], observer.Succeeded);
    }

    [Fact]
    public async Task EmptyList_DoesNothingAndReportsNothing()
    {
        var winget = new FakeWinget(_ => Ok());
        var observer = new RecordingObserver();

        UpgradeBatchOutcome result = await RunAsync(winget, [], observer);

        Assert.Equal(0, result.Succeeded);
        Assert.Empty(winget.Calls);
        Assert.Empty(observer.Started);
    }

    // ── Fallos ──────────────────────────────────────────────────────────────

    /// <summary>Un fallo NO detiene el lote: era el bug que motivó el resumen único de fallos.</summary>
    [Fact]
    public async Task OnePackageFails_TheRestAreStillAttempted()
    {
        var winget = new FakeWinget(id => id == "B" ? Fails("network error") : Ok());
        var observer = new RecordingObserver();
        var packages = new[] { Package("A"), Package("B"), Package("C") };

        UpgradeBatchOutcome result = await RunAsync(winget, packages, observer);

        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.False(result.Cancelled);
        Assert.Equal(["A", "B", "C"], winget.Calls);
    }

    /// <summary>El motivo que se acumula es el ya traducido, no la salida cruda de winget.</summary>
    [Fact]
    public async Task AFailure_CarriesTheTranslatedReasonAndTheIdentityOfThePackage()
    {
        var winget = new FakeWinget(_ => Fails("network error while downloading"));
        var observer = new RecordingObserver();

        UpgradeBatchOutcome result = await RunAsync(winget, [Package("A")], observer);

        FailedUpgrade failure = Assert.Single(result.Failures);
        Assert.Equal("A", failure.Id);
        Assert.Equal("A App", failure.Name);
        Assert.Equal(L.T("reason.networkError"), failure.Reason);
        Assert.Equal(failure.Reason, Assert.Single(observer.Failed).Reason);
    }

    [Fact]
    public async Task EveryPackageFails_TheCountMatchesTheListOfFailures()
    {
        var winget = new FakeWinget(_ => Fails("blocked by policy"));
        var observer = new RecordingObserver();
        var packages = new[] { Package("A"), Package("B") };

        UpgradeBatchOutcome result = await RunAsync(winget, packages, observer);

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(2, result.Failed);
        Assert.Equal(result.Failed, result.Failures.Count);
    }

    // ── Cancelación ─────────────────────────────────────────────────────────

    /// <summary>
    /// Cancelar significa parar: lo que ya se hizo se conserva, lo que faltaba no se intenta.
    /// </summary>
    [Fact]
    public async Task CancellingMidBatch_StopsAndKeepsWhatWasAlreadyDone()
    {
        using var cts = new CancellationTokenSource();
        var observer = new RecordingObserver();

        // Cancela justo antes de atender a "B": el primero ya está hecho, el tercero no debe tocarse.
        var winget = new FakeWinget(_ => Ok())
        {
            BeforeEachCall = id => { if (id == "B") cts.Cancel(); }
        };
        var packages = new[] { Package("A"), Package("B"), Package("C") };

        UpgradeBatchOutcome result = await RunAsync(winget, packages, observer, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(["A"], winget.Calls);
        Assert.DoesNotContain("C", observer.Started);
    }

    /// <summary>
    /// Winget puede terminar bien justo mientras el usuario cancela. Sin la comprobación del token
    /// posterior a la llamada, el lote seguiría con el paquete siguiente después del clic en Cancelar.
    /// </summary>
    [Fact]
    public async Task PackageFinishesRightAsCancellationArrives_TheNextOneIsNotStarted()
    {
        using var cts = new CancellationTokenSource();
        var observer = new RecordingObserver();

        // No lanza OperationCanceledException: devuelve éxito y deja el token ya cancelado.
        var winget = new FakeWinget(id => { if (id == "A") cts.Cancel(); return Ok(); });
        var packages = new[] { Package("A"), Package("B") };

        UpgradeBatchOutcome result = await RunAsync(winget, packages, observer, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(["A"], winget.Calls);
    }

    [Fact]
    public async Task AlreadyCancelledBeforeStarting_NothingIsAttempted()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var winget = new FakeWinget(_ => Ok());
        var observer = new RecordingObserver();

        UpgradeBatchOutcome result = await RunAsync(winget, [Package("A")], observer, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(0, result.Succeeded);
        Assert.Empty(winget.Calls);
    }

    // ── Lo que se le cuenta a la ventana ────────────────────────────────────

    [Fact]
    public async Task ProgressAndLogLines_ReachTheObserver()
    {
        using var _ = InlineSynchronizationContext.Install();

        var winget = new FakeWinget(_ => Ok())
        {
            ProgressLines = ["Descargando…", "Instalando…"],
            ProgressToReport = new WingetProgressInfo(512, 1024, 0)
        };
        var observer = new RecordingObserver();

        await RunAsync(winget, [Package("A"), Package("B")], observer);

        Assert.Equal(4, observer.LogLines.Count);          // 2 líneas × 2 paquetes
        Assert.Equal([0, 1], observer.ProgressIndices);    // cada progreso, con SU índice
    }

    /// <summary>
    /// El índice que acompaña al progreso identifica al paquete en curso. Capturar la variable del
    /// bucle en vez de una copia haría que un callback tardío pintara el nombre del paquete siguiente.
    /// </summary>
    [Fact]
    public async Task TheProgressIndex_MatchesThePackageBeingUpdated()
    {
        using var _ = InlineSynchronizationContext.Install();

        var seen = new List<(int Index, string Id)>();
        var winget = new FakeWinget(_ => Ok())
        {
            ProgressToReport = new WingetProgressInfo(1, 2, 0)
        };
        var observer = new DelegateObserver((index, package) => seen.Add((index, package.Id)));

        await RunAsync(winget, [Package("A"), Package("B"), Package("C")], observer);

        Assert.Equal([(0, "A"), (1, "B"), (2, "C")], seen);
    }

    private sealed class DelegateObserver(Action<int, WingetPackage> onProgress) : IUpgradeBatchObserver
    {
        public void DownloadProgress(int index, int total, WingetPackage package, WingetProgressInfo info) =>
            onProgress(index, package);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheSilentFlag_ReachesWingetUnchanged(bool silent)
    {
        var winget = new FakeWinget(_ => Ok());

        await RunAsync(winget, [Package("A")], new RecordingObserver(), silent: silent);

        Assert.Equal([silent], winget.SilentFlags);
    }

    // ── Contrato ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NullArguments_AreRejectedInsteadOfFailingLater() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            UpgradeBatchRunner.RunAsync(null!, [], silent: true, new RecordingObserver(), CancellationToken.None));
}
