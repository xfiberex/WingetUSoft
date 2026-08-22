using System.Collections.Concurrent;

namespace WingetUSoft;

/// <summary>
/// Escritor del registro diario a disco. Encola en el hilo de UI y escribe desde una tarea de fondo.
/// </summary>
/// <remarks>
/// <para>
/// Antes, cada línea del registro hacía <c>Directory.CreateDirectory</c> + <c>File.AppendAllText</c>
/// —abrir, escribir y cerrar el archivo— de forma **síncrona en el hilo de UI** y dentro de un
/// <c>lock</c>. Durante una actualización eso no es una línea de vez en cuando: la app retransmite
/// **toda** la salida de winget, así que un lote de diez paquetes abría y cerraba el archivo cientos
/// de veces, cada una parando el hilo que tiene que repintar la barra de progreso.
/// </para>
/// <para>
/// Aquí la escritura pasa a una sola tarea consumidora, que mantiene el <see cref="StreamWriter"/>
/// abierto mientras haya actividad y lo cierra tras un rato de silencio. Un lote completo se escribe
/// con **una sola apertura**, y el hilo de UI solo paga encolar una cadena.
/// </para>
/// <para>
/// La marca de tiempo se pone al **encolar**, no al escribir: si se pusiera al escribir, un pico de
/// escritura desplazaría las horas del registro respecto a cuándo pasaron las cosas de verdad.
/// Un único consumidor conserva además el orden de las líneas.
/// </para>
/// </remarks>
internal sealed class FileLog : IDisposable
{
    /// <summary>Tras este silencio se suelta el archivo: nadie escribe, nadie retiene el handle.</summary>
    private static readonly TimeSpan IdleBeforeClosing = TimeSpan.FromSeconds(5);

    private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
    private readonly Action<string> _onFailure;
    private readonly Task _pump;

    private StreamWriter? _writer;
    private string? _openPath;

    /// <summary>
    /// Cuántas veces se ha abierto el archivo. Es el seam con el que se comprueba lo que de verdad
    /// arregla esta clase: que un lote entero no abra el archivo una vez por línea.
    /// </summary>
    internal int FileOpenCount;

    /// <summary>Se apaga solo al primer fallo de E/S; a partir de ahí no se reintenta nada.</summary>
    internal bool IsAvailable { get; private set; } = true;

    /// <param name="onFailure">
    /// Se invoca una sola vez, desde la tarea de fondo, con el mensaje del fallo que apagó el registro.
    /// Quien lo reciba es responsable de saltar al hilo de UI si va a tocar la interfaz.
    /// </param>
    internal FileLog(Action<string> onFailure)
    {
        _onFailure = onFailure;
        _pump = Task.Run(Pump);
    }

    /// <summary>Encola una línea. No toca el disco: vuelve de inmediato.</summary>
    internal void Write(string text)
    {
        if (!IsAvailable || _queue.IsAddingCompleted) return;

        try { _queue.Add($"[{DateTime.Now:HH:mm:ss}] {text}"); }
        catch (InvalidOperationException) { /* se cerró la cola mientras tanto */ }
    }

    private void Pump()
    {
        try
        {
            while (true)
            {
                // Con el archivo cerrado se espera indefinidamente (no hay handle que soltar); con el
                // archivo abierto, solo hasta que el silencio justifique cerrarlo.
                int wait = _writer is null ? Timeout.Infinite : (int)IdleBeforeClosing.TotalMilliseconds;

                if (!_queue.TryTake(out string? line, wait))
                {
                    if (_queue.IsCompleted) return;   // Dispose: no queda nada por escribir
                    CloseWriter();                    // silencio: se suelta el archivo y se vuelve a esperar
                    continue;
                }

                if (!EnsureOpen()) return;
                _writer!.WriteLine(line);

                // Se vacía la ráfaga entera antes de tocar el disco: es lo que convierte cientos de
                // aperturas en una.
                while (_queue.TryTake(out string? more))
                    _writer.WriteLine(more);

                _writer.Flush();
            }
        }
        catch (Exception ex)
        {
            Fail(ex.Message);
        }
        finally
        {
            CloseWriter();
        }
    }

    /// <summary>
    /// Abre el archivo del día si hace falta. El nombre lleva la fecha, así que una sesión que cruce la
    /// medianoche tiene que pasar sola al archivo siguiente.
    /// </summary>
    private bool EnsureOpen()
    {
        string path = Path.Combine(AppSettings.LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

        if (_writer is not null && _openPath == path) return true;
        CloseWriter();

        try
        {
            Directory.CreateDirectory(AppSettings.LogDirectory);
            _writer = new StreamWriter(path, append: true) { AutoFlush = false };
            _openPath = path;
            Interlocked.Increment(ref FileOpenCount);
            return true;
        }
        catch (Exception ex)
        {
            Fail(ex.Message);
            return false;
        }
    }

    private void CloseWriter()
    {
        try { _writer?.Dispose(); } catch { /* al cerrar ya no hay nada que salvar */ }
        _writer = null;
        _openPath = null;
    }

    private void Fail(string message)
    {
        if (!IsAvailable) return;
        IsAvailable = false;
        try { _onFailure(message); } catch { }
    }

    /// <summary>Cierra la cola y espera a que se escriba lo pendiente: nada se pierde al salir.</summary>
    public void Dispose()
    {
        try { _queue.CompleteAdding(); } catch { }
        try { _pump.Wait(TimeSpan.FromSeconds(5)); } catch { }
        _queue.Dispose();
    }
}
