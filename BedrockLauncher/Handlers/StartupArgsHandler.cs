using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Handlers
{
    public class StartupArgsHandler
    {

        #region Public Accessors
        private static string[] StartupArgs = new string[0];
        public static void SetStartupArgs(string[] args) => StartupArgs = args;
        public static void RunStartupArgs() => PraseArgs(StartupArgs);
        #endregion

        #region Messages
        private const string HELP_ARG = "--help";
        private const string NO_WINDOW_ARG = "--nowindow";
        private const string LAUNCH_INSTALLATION_ARG = "--launch";
        private const string CLOSE_ON_LAUNCH_ARG = "--closeOnLaunch";
        private const string PRESIST_ON_LAUNCH_ARG = "--keepOpenOnLaunch";

        private const string WRONG_ARGUMENT_MESSAGE = "Wrong argument, try --help, argument given:";

        private static List<string> HELP_MESSAGE = new List<string>()
        {
             "Avaliable Arguments:",
             string.Format("{0} - {1}", HELP_ARG, "Show avaliable arguments"),
             string.Format("{0} - {1}", NO_WINDOW_ARG, "Hide main window from showing up (WARNING: Will only be killable using task manager)"),
             string.Format("{0} - {1}", CLOSE_ON_LAUNCH_ARG, string.Format("Close launcher after launch when using the '{0}' argument", LAUNCH_INSTALLATION_ARG)),
             string.Format("{0} - {1}", PRESIST_ON_LAUNCH_ARG, string.Format("Keep launcher open after launch when using the '{0}' argument", LAUNCH_INSTALLATION_ARG)),
             string.Format("{0} - {1} ({0} {2})", LAUNCH_INSTALLATION_ARG, "Launch installation specified", "<profileName> <installationName>")
        };
        #endregion

        private static bool CloseOnLaunch { get; set; } = false;
        private static bool KeepOpenOnLaunch { get; set; } = false;

        private static void PraseArgs(string[] args)
        {
            if (args == null) return;

            bool KillApp = false;
            bool EndPrase = false;

            for (int i = 0; i < args.Length; i++)
            {
                string argument = args[i];
                switch (argument)
                {
                    case NO_WINDOW_ARG:
                        HideWindow();
                        break;
                    case CLOSE_ON_LAUNCH_ARG:
                        CloseOnLaunch = true;
                        break;
                    case PRESIST_ON_LAUNCH_ARG:
                        KeepOpenOnLaunch = true;
                        break;
                    case LAUNCH_INSTALLATION_ARG:
                        bool result = LaunchInstallation(args, i);
                        if (!result) KillApp = true;
                        else EndPrase = true;
                        goto EscapeLoop;
                    case HELP_ARG:
                        ShowHelp();
                        KillApp = true;
                        goto EscapeLoop;
                    default:
                        InvalidMessage(argument);
                        KillApp = true;
                        goto EscapeLoop;
                }
            }

        EscapeLoop:

            if (EndPrase) return;
            else if (KillApp) Application.Current.MainWindow.Close();
        }
        private static void InvalidMessage(string argument)
        {
            Console.WriteLine(string.Format("{0} {1}", WRONG_ARGUMENT_MESSAGE, argument));
        }
        private static void ShowHelp()
        {
            Console.WriteLine(String.Join(Environment.NewLine, HELP_MESSAGE));
        }
        private static bool LaunchInstallation(string[] args, int index)
        {
            int count = args.Length - 1;
            if (index + 2 <= count)
            {
                var profiles = ViewModels.MainViewModel.Default.Config.profiles;
                string profileName = args[index + 1];
                string installationName = args[index + 2];

                //Profiles Not Found
                if (profiles == null) return false;
                //Profile Not Found
                else if (!profiles.ContainsKey(profileName)) return false;
                //Installation Not Found
                else if (!profiles[profileName].Installations.Any(x => x.DisplayName == installationName)) return false;
                //Launch Installation
                else
                {
                    var p = profiles[profileName];
                    var i = p.Installations.Where(x => x.DisplayName == installationName).FirstOrDefault();
                    bool c = Properties.LauncherSettings.Default.KeepLauncherOpen;
                    if (KeepOpenOnLaunch) c = true;
                    else if (CloseOnLaunch) c = false;   
                    ViewModels.MainViewModel.Default.Play(p, i, c, false);
                    return true;
                }
            }
            //Not Enough Args
            else return false;
        }
        private static void HideWindow()
        {
            Application.Current.MainWindow.Hide();
        }
    }
}
