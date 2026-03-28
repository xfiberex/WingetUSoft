namespace WingetUSoft
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (WingetService.IsElevatedWorkerInvocation(args))
                return WingetService.RunElevatedBatchWorkerAsync(args).GetAwaiter().GetResult();

            ApplicationConfiguration.Initialize();
            Application.Run(new FormApp());
            return 0;
        }
    }
}