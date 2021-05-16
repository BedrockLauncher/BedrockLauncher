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
using BedrockLauncher.Interfaces;
using BedrockLauncher.Methods;
using BedrockLauncher.Classes;
using System.Windows.Media.Animation;
using BedrockLauncher.Pages;
using BedrockLauncher.Pages.FirstLaunch;
using BedrockLauncher.ViewModels;
using BedrockLauncher.Pages.Settings;
using BedrockLauncher.Pages.Play;
using BedrockLauncher.Pages.News;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Common;
using BedrockLauncher.Pages.Community;

namespace BedrockLauncher
{
    //TODO: Improve backup overwrite response (to avoid having to click it forever)
    //TODO: Add translation support to the remaining entries in GameManager.cs
    //TODO: (Later On) Better Animations
    //TODO: (Later On) Community Content / Personal Donations Section


    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) Init();
        }
        private void Init()
        {
            Panel.SetZIndex(OverlayFrame, 0);
            Panel.SetZIndex(ErrorFrame, 1);
            Panel.SetZIndex(updateButton, 2);

            BL_Core.Language.LanguageManager.Init();
            ConfigManager.ViewModel.Init();

            this.DataContext = ConfigManager.ViewModel;
        }
        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.GameManager.GameProcess != null) ConfigManager.GameManager.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as MCInstallation;
                ConfigManager.GameManager.Play(i);
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
        private void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigManager.ViewModel.RefreshSkinsPage();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigManager.ViewModel.AttemptClose(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.GameManager.Cancel();
        }
    }
}
