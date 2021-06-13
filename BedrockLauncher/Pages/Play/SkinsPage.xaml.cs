using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Core.Classes.SkinPack;
using BedrockLauncher.Methods;
using System.IO;
using System.Data;
using BedrockLauncher.Controls.Items;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.IO.Compression;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Core.Pages.Common;
using ExtensionsDotNET;
using BedrockLauncher.Classes;
using BedrockLauncher.Core.Classes;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;

namespace BedrockLauncher.Pages.Play
{
    /// <summary>
    /// Interaction logic for SkinsPage.xaml
    /// </summary>
    public partial class SkinsPage : Page
    {
        private bool HasLoadedOnce = false;
        public ObservableCollection<MCSkinPack> SkinPacks { get; set; } = new ObservableCollection<MCSkinPack>();
        public ObservableCollection<MCSkin> Skins { get; set; } = new ObservableCollection<MCSkin>();


        #region Init

        public SkinsPage()
        {
            InitializeComponent();
        }
        private void Page_Initialized(object sender, EventArgs e)
        {

        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!HasLoadedOnce)
                {
                    LoadedSkinPacks.ItemsSource = SkinPacks;
                    SkinPreviewList.ItemsSource = Skins;
                    InstallationsList.ItemsSource = LauncherModel.Default.ConfigManager.CurrentInstallations;
                    var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                    view.Filter = LauncherModel.Default.ConfigManager.Filter_InstallationList;
                    HasLoadedOnce = true;
                }
                RefreshInstallationsList();
                ReloadSkinPacks();
            });
        }


        #endregion

        #region UI
        private async void RefreshInstallationsList()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                var view = CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource) as CollectionView;
                view.Refresh();
            });
        }
        public async void ReloadSkinPacks()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                SkinPacks.Clear();

                var installation = LauncherModel.Default.ConfigManager.CurrentInstallation;

                if (installation == null) return;

                string InstallationPath = LauncherModel.Default.FilepathManager.GetInstallationsFolderPath(LauncherModel.Default.ConfigManager.CurrentProfile, installation.DirectoryName);
                string normal_folder = LauncherModel.Default.FilepathManager.GetSkinPacksFolderPath(InstallationPath, false);
                string dev_folder = LauncherModel.Default.FilepathManager.GetSkinPacksFolderPath(InstallationPath, true);

                if (Directory.Exists(normal_folder)) AddPacks(normal_folder);
                if (Directory.Exists(dev_folder)) AddPacks(dev_folder);

                UpdateAddSkinButton();

                void AddPacks(string _SourceFolder)
                {
                    var SourceFolder = new DirectoryInfo(_SourceFolder);
                    var FoundFolders = SourceFolder.GetDirectories();
                    foreach (var PossiblePack in FoundFolders)
                    {
                        if (PossiblePack.GetFiles().ToList().Exists(x => x.Name == "manifest.json"))
                        {
                            var result = MCSkinPack.ValidatePack(PossiblePack.FullName);
                            if (result != null)
                            {
                                SkinPacks.Add(result);
                            }
                        }


                    }
                }
            });
        }
        private async void ReloadSkins()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                if (!this.IsInitialized) return;
                Skins.Clear();
                var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;
                if (selected_item != null)
                {
                    foreach (var skin in selected_item.Content.skins)
                    {
                        Skins.Add(skin);
                    }
                }
            });
        }
        private void UpdateAddSkinButton()
        {
            var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;
            AddSkinButton.IsEnabled = selected_item != null;
        }
        private void UpdateCurrentSkin()
        {
            var selected_skin = SkinPreviewList.SelectedItem as MCSkin;
            var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;

            if (selected_skin != null && selected_item != null)
            {
                CurrentSkinNameTextBlock.Text = selected_item.GetLocalizedSkinName(selected_skin.localization_name);
                SkinPreviewPanel.UpdateSkin(selected_skin);
            }
            else
            {
                CurrentSkinNameTextBlock.Text = "NULL";
                SkinPreviewPanel.UpdateSkin();
            }
        }
        #endregion

        #region Selections

        private async void LoadedSkinPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                ReloadSkins();
                UpdateCurrentSkin();
                UpdateAddSkinButton();
            });
        }
        private async void SkinPreviewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                UpdateCurrentSkin();
                UpdateAddSkinButton();
            });
        }
        private async void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await Task.Run(ReloadSkinPacks);
        }

        #endregion

        #region Clicks
        private void AddSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditSkinScreen(skinPack));
        }
        private void ImportSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "MCPACK Files (*.mcpack)|*.mcpack|ZIP Files (*.zip)|*.zip"
            };
            if (dialog.ShowDialog().Value)
            {
                try
                {
                    var file = ZipFile.OpenRead(dialog.FileName);
                    if (file.Entries.ToList().Exists(x => x.FullName == "skins.json"))
                    {
                        file.Dispose();
                        string InstallationPath = LauncherModel.Default.FilepathManager.GetInstallationsFolderPath(LauncherModel.Default.ConfigManager.CurrentProfile, LauncherModel.Default.ConfigManager.CurrentInstallation.DirectoryName);
                        string NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        string NewPackDirectory = Path.Combine(LauncherModel.Default.FilepathManager.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);

                        while (Directory.Exists(NewPackDirectory))
                        {
                            NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                            NewPackDirectory = Path.Combine(LauncherModel.Default.FilepathManager.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);
                        }

                        ZipFile.ExtractToDirectory(dialog.FileName, NewPackDirectory);
                    }
                    else
                    {
                        file.Dispose();
                        ErrorScreenShow.errormsg("notaskinpack");
                    }
                }
                catch (Exception ex)
                {
                    ErrorScreenShow.exceptionmsg(ex);
                }

            }
            ReloadSkinPacks();
        }
        private void NewSkinPackButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.LauncherModel.Default.SetOverlayFrame(new EditSkinPackScreen());
        }
        private void SkinPreviewList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            ListViewItem lbi = (SkinPreviewList.ItemContainerGenerator.ContainerFromIndex(SkinPreviewList.SelectedIndex)) as ListViewItem;
            ContentPresenter cp = WPFExtensions.FindVisualChild<ContentPresenter>(lbi);
            DataTemplate dt = lbi.ContentTemplate;
            SkinItem item = (dt.FindName("ItemControl", cp)) as SkinItem;

            if (item != null && SkinPreviewList.IsKeyboardFocusWithin)
            {
                item.OpenContextMenu();
            }
        }
        private void LoadedSkinPacks_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        #endregion
    }
}
