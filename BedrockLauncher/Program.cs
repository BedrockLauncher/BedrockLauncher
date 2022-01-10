using System.Diagnostics;
using System;

namespace BedrockLauncher
{
    public static class Program
    {

        [STAThread]
        public static void Main()
        {
            Internals.StartLogging();
            Internals.ValidateOSArchitecture();
            Debug.WriteLine("Application Starting...");

            var application = new App();
            application.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            application.DispatcherUnhandledException += Internals.Error_Unhandled;
            application.Startup += Internals.OnStartup;
            application.InitializeComponent();
            application.Run();
        }
    }
}
