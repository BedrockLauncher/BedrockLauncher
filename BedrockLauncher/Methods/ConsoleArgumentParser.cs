using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BedrockLauncher.Methods
{
    class ConsoleArgumentParser
    {
        private const string WRONG_ARGUMENT_MESSAGE = "Wrong argument, try --help, argument given: ";
        private const string HELP_MESSAGE = "All arguments:\n" +
            "'--nowindow' - hide main window from showing up\n";
        public ConsoleArgumentParser(string[] args)
        {
            Debug.WriteLine("Launched with arguments: ");
            foreach (string argument in args)
            {
                if (!argument.StartsWith("--")) { Console.WriteLine(WRONG_ARGUMENT_MESSAGE + argument); }
                //if (argument == "--help")
            }

        }

        private void launchWithoutWindow()
        {
            Application.Current.MainWindow.Hide();
        }
    }
}
