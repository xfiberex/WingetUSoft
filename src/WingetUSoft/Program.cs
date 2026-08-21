using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WingetUSoft;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (WingetService.IsElevatedWorkerInvocation(args))
                return WingetService.RunElevatedBatchWorkerAsync(args).GetAwaiter().GetResult();

            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
            return 0;
        }
        catch (Exception ex)
        {
            // Mismo tratamiento que en App.UnhandledException: se añade al historial, no se pisa.
            // Aquí no hay recuperación posible — el proceso ya viene de vuelta de Main —, así que
            // siempre se avisa antes de salir con código 1.
            CrashLog.Write("Program.Main", ex);

            // El worker elevado corre sin interfaz y sin nadie mirando: un MessageBox ahí no lo
            // vería el usuario y dejaría el proceso colgado esperando un clic que no llega.
            if (!WingetService.IsElevatedWorkerInvocation(args))
                CrashLog.Notify();
            return 1;
        }
    }
}