using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;
using BedrockLauncher.Classes;
using System.Windows.Media.Animation;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.Welcome;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.Play;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Handlers;
using BedrockLauncher.UI.Pages.Common;
using BedrockLauncher.UI.Components;

namespace BedrockLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = MainViewModel.Default;
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MainViewModel.Default.AttemptClose(sender, e);
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            Panel.SetZIndex(MainFrame, 0);
            Panel.SetZIndex(OverlayFrame, 1);
            Panel.SetZIndex(ErrorFrame, 2);
            Panel.SetZIndex(UpdateButton, 3);

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                await Program.OnApplicationLoaded();
                MainPage.NavigateToGamePage();
                StartupArgsHandler.RunStartupArgs();

                bool isFirstLaunch = Properties.LauncherSettings.Default.GetIsFirstLaunch(MainDataModel.Default.Config.profiles.Count());
                if (isFirstLaunch) MainViewModel.Default.SetOverlayFrame(new WelcomePage(), true);
            }
        }


    }
}
