using System.Collections.Concurrent;
using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// En qué hilo avisa el lote a la ventana.
/// </summary>
/// <remarks>
/// <para>
/// Desde la v1.8.8 (T4-03) el bucle esperaba a winget con <c>ConfigureAwait(false)</c>. Tras el primer paquete, los
/// avisos siguientes —«correcto», «falló», «empieza el siguiente»— llegaban desde un hilo de fondo, y la ventana
/// toca controles en ellos: la excepción cortaba el lote en cuanto terminaba el primer paquete y salía un «Error
/// de actualización» vacío. Se vio al probar F-19 con un lote real. Lo tapaban dos cosas: el modo administrador no
/// pasa por este bucle, y los demás tests del lote instalan un contexto que ejecuta todo en línea.
/// </para>
/// <para>
/// Este test usa un contexto con un único hilo propio, como la cola de la interfaz, y un winget que termina de
/// verdad en otro hilo.
/// </para>
/// </remarks>
public sealed class UpgradeBatchThreadingTests
{
    /// <summary>Contexto de un solo hilo con su propia cola, como el de la interfaz.</summary>
    private sealed class SingleThreadContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly Thread _thread;

        public SingleThreadContext()
        {
            _thread = new Thread(() =>
            {
                SetSynchronizationContext(this);
                foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                    callback(state);
            })
            { IsBackground = true, Name = "cola de la interfaz (test)" };
            _thread.Start();
        }

        public int ThreadId => _thread.ManagedThreadId;

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

        /// <summary>Ejecuta <paramref name="work"/> dentro de la cola y espera a que acabe.</summary>
        public Task RunAsync(Func<Task> work)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(async _ =>
            {
                try { await work(); done.SetResult(); }
                catch (Exception ex) { done.SetException(ex); }
            }, null);
            return done.Task;
        }

        public void Dispose() => _queue.CompleteAdding();
    }

    /// <summary>Winget que responde de verdad más tarde y desde el pool de hilos, como el proceso real.</summary>
    private sealed class SlowWinget(Func<string, UpgradeResult> responder) : IWingetService
    {
        public async Task<UpgradeResult> UpgradePackageAsync(
            string packageId,
            bool silent,
            IProgress<WingetProgressInfo>? progress,
            CancellationToken cancellationToken,
            IProgress<string>? logProgress)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return responder(packageId);
        }
    }

    private sealed class ThreadRecordingObserver : IUpgradeBatchObserver
    {
        public ConcurrentQueue<(string Call, int ThreadId)> Calls { get; } = new();

        public void PackageStarting(int index, int total, WingetPackage package) => Record("empieza " + package.Id);
        public void PackageSucceeded(WingetPackage package) => Record("correcto " + package.Id);
        public void PackageFailed(WingetPackage package, string reason) => Record("falló " + package.Id);

        private void Record(string call) => Calls.Enqueue((call, Environment.CurrentManagedThreadId));
    }

    private static WingetPackage Package(string id) =>
        new() { Name = id + " App", Id = id, Version = "1.0", Available = "2.0", Source = "winget" };

    [Fact]
    public async Task EveryNotification_ReachesTheObserverOnTheCallersThread()
    {
        using var ui = new SingleThreadContext();
        var observer = new ThreadRecordingObserver();
        var winget = new SlowWinget(id => id == "B"
            ? new UpgradeResult { Success = false, ExitCode = 1, ErrorOutput = "error" }
            : new UpgradeResult { Success = true, ExitCode = 0 });

        await ui.RunAsync(() => UpgradeBatchRunner.RunAsync(
            winget, [Package("A"), Package("B"), Package("C")], silent: true, observer, CancellationToken.None));

        var calls = observer.Calls.ToList();
        Assert.Equal(6, calls.Count);   // empieza y termina cada uno de los tres
        var offThread = calls.Where(c => c.ThreadId != ui.ThreadId).Select(c => c.Call).ToList();
        Assert.True(offThread.Count == 0,
            "Avisos fuera del hilo de la interfaz (la ventana reventaría al tocar un control): " + string.Join(", ", offThread));
    }
}
