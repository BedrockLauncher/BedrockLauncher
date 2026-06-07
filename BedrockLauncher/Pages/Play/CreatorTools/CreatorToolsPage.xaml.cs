using BedrockLauncher.Classes;
using BedrockLauncher.Pages.Play.Installations.Components;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.ViewModels;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BedrockLauncher.Pages.Play.CreatorTools
{
    public partial class CreatorToolsPage : Page
    {
        public CreatorToolsPage()
        {
            InitializeComponent();
            InstallationsList.SelectionChanged += CheckVersionAvailability;
            Loaded += CreatorToolsPage_Loaded;

            if (MainDataModel.Default.Config.CurrentInstallations is INotifyCollectionChanged installations)
                installations.CollectionChanged += (sender, e) => QueueEditorStateRefresh();

            MainDataModel.Default.Versions.CollectionChanged += (sender, e) => QueueEditorStateRefresh();

            ((INotifyPropertyChanged)MainDataModel.Default.ProgressBarState).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainDataModel.Default.ProgressBarState.AllowPlaying))
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CheckVersionAvailability(s, e);
                    });
            };
        }

        private void CreatorToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            QueueEditorStateRefresh();
        }

        private void CheckVersionAvailability(object _, EventArgs __)
        {
            BLInstallation selectedInstallation = InstallationsList.SelectedItem as BLInstallation ?? MainDataModel.Default.Config.CurrentInstallation;

            if (MainDataModel.Default.PackageManager.isGameRunning)
            {
                RestoreEditorButton(true);
                EditorPlayButton.IsEnabled = true;
            }
            else if (selectedInstallation is not null && selectedInstallation.Version is null)
            {
                EditorPlayButton.IsEnabled = false;
            }
            else if (!IsEditorEligible(selectedInstallation))
            {
                SetEditorNotEligibleButton();
            }
            else
            {
                RestoreEditorButton();
                EditorPlayButton.IsEnabled = MainDataModel.Default.ProgressBarState.AllowPlaying;
            }
        }

        private void MainPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataModel.Default.PackageManager.isGameRunning) MainDataModel.Default.KillGame();
            else
            {
                var i = InstallationsList.SelectedItem as BLInstallation ?? MainDataModel.Default.Config.CurrentInstallation;
                if (i?.IsPlayableInstalled != true)
                    return;
                if (!IsEditorEligible(i))
                    return;

                bool KeepLauncherOpen = Properties.LauncherSettings.Default.KeepLauncherOpen;
                MainDataModel.Default.Play(ViewModels.MainDataModel.Default.Config.CurrentProfile, i, KeepLauncherOpen, true);
            }
        }

        private void QueueEditorStateRefresh()
        {
            Dispatcher.BeginInvoke(new Action(() => CheckVersionAvailability(this, EventArgs.Empty)), DispatcherPriority.Background);
        }

        private bool IsEditorEligible(BLInstallation installation)
        {
            if (installation?.Version == null) return false;
            return installation.Version.Compare(Constants.GetMinimumEditorVersion(installation.VersionType)) <= 0;
        }

        private void SetEditorNotEligibleButton()
        {
            BindingOperations.ClearBinding(EditorPlayButton, Button.IsEnabledProperty);
            BindingOperations.ClearBinding(PlayButtonText, TextBlock.TextProperty);

            EditorPlayButton.Style = (Style)FindResource("BigUnavailableEditorButton");
            EditorPlayButton.Margin = new Thickness(286, 0, 286, 0);
            EditorPlayButton.IsEnabled = true;
            EditorPlayButton.IsHitTestVisible = false;
            EditorPlayButton.Focusable = false;

            PlayButtonText.Text = "You're not eligible for this feature.";
            PlayButtonText.FontSize = 12;
            PlayButtonText.TextWrapping = TextWrapping.Wrap;
            PlayButtonText.TextAlignment = TextAlignment.Center;
            PlayButtonText.Margin = new Thickness(8, -1, 8, 0);
            PlayButtonText.Foreground = new SolidColorBrush(Color.FromRgb(242, 242, 242));
            PlayButtonText.Effect = null;
        }

        private void RestoreEditorButton(bool forceEnabled = false)
        {
            EditorPlayButton.Style = (Style)FindResource("BigGreenButton");
            EditorPlayButton.Margin = new Thickness(286, -8, 286, 0);
            EditorPlayButton.IsHitTestVisible = true;
            EditorPlayButton.Focusable = true;

            if (forceEnabled)
            {
                BindingOperations.ClearBinding(EditorPlayButton, Button.IsEnabledProperty);
                EditorPlayButton.IsEnabled = true;
            }
            else
            {
                BindingOperations.SetBinding(EditorPlayButton, Button.IsEnabledProperty, new Binding("ProgressBarState.AllowPlaying")
                {
                    Source = MainDataModel.Default,
                    Mode = BindingMode.OneWay
                });
            }

            BindingOperations.SetBinding(PlayButtonText, TextBlock.TextProperty, new Binding("ProgressBarState.PlayEditorButtonString")
            {
                Source = MainDataModel.Default,
                Mode = BindingMode.OneWay
            });

            PlayButtonText.ClearValue(TextBlock.ForegroundProperty);
            PlayButtonText.ClearValue(TextBlock.FontSizeProperty);
            PlayButtonText.ClearValue(TextBlock.TextWrappingProperty);
            PlayButtonText.ClearValue(TextBlock.TextAlignmentProperty);
            PlayButtonText.ClearValue(TextBlock.MarginProperty);
            PlayButtonText.ClearValue(TextBlock.EffectProperty);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e) { }
    }
}
