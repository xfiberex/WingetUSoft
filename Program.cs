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
            try
            {
                string crashDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WingetUSoft");
                Directory.CreateDirectory(crashDir);
                File.WriteAllText(Path.Combine(crashDir, "crash.log"), ex.ToString());
            }
            catch { }
            return 1;
        }
    }
}