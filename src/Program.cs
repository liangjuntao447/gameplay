using System;
using System.Threading;
using System.Windows;

namespace TouchCloudPad
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Never let an unhandled exception silently kill the overlay:
            // log it and, for dispatcher exceptions, keep the app alive.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                CrashLog.Write(e.ExceptionObject as Exception);

            // single instance guard
            bool createdNew;
            using (var mutex = new Mutex(true, "TouchCloudPad_SingleInstance", out createdNew))
            {
                if (!createdNew) return; // another instance is already running
                var app = new Application();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    CrashLog.Write(e.Exception);
                    e.Handled = true; // keep running; don't crash the overlay
                };
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(new MainWindow());
            }
        }
    }
}
