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
using BL_Core.Classes.SkinPack;
using BedrockLauncher.Methods;
using System.IO;
using System.Data;
using BedrockLauncher.Controls.Items;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.IO.Compression;
using BedrockLauncher.Pages.Preview;
using BL_Core.Pages.Common;
using ExtensionsDotNET;
using BedrockLauncher.Classes;
using BL_Core.Classes;

namespace BedrockLauncher.Pages.Play
{
    /// <summary>
    /// Interaction logic for SkinsPage.xaml
    /// </summary>
    public partial class SkinsPage : Page
    {

        public List<MCSkinPack> SkinPacks { get; set; } = new List<MCSkinPack>();

        public SkinsPage()
        {
            InitializeComponent();
            ConfigManager.Default.ConfigStateChanged += ConfigManager_ConfigStateChanged;
        }

        private async void ConfigManager_ConfigStateChanged(object sender, EventArgs e)
        {
            await Task.Run(() => {
                RefreshInstallations();
                ReloadSkinPacks();
            });
        }

        private async void RefreshInstallations()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                InstallationsList.Items.Cast<MCInstallation>().ToList().ForEach(x => x.Update());
                if (InstallationsList.SelectedItem == null) InstallationsList.SelectedItem = ConfigManager.Default.CurrentInstallations.First();
                if (InstallationsList.ItemsSource != null) CollectionViewSource.GetDefaultView(InstallationsList.ItemsSource).Refresh();
                else InstallationsList.ItemsSource = ConfigManager.Default.CurrentInstallations;
            });
        }

        public async void ReloadSkinPacks()
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                LoadedSkinPacks.Items.Clear();
                SkinPacks.Clear();

                var installation = ConfigManager.Default.CurrentInstallation;

                if (installation == null) return;

                string InstallationPath = FilepathManager.Default.GetInstallationsFolderPath(ConfigManager.Default.CurrentProfile, installation.DirectoryName);
                string normal_folder = FilepathManager.Default.GetSkinPacksFolderPath(InstallationPath, false);
                string dev_folder = FilepathManager.Default.GetSkinPacksFolderPath(InstallationPath, true);

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
                                LoadedSkinPacks.Items.Add(result);
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
                SkinPreviewList.Items.Clear();
                var selected_item = LoadedSkinPacks.SelectedItem as MCSkinPack;
                if (selected_item != null)
                {
                    foreach (var skin in selected_item.Content.skins)
                    {
                        SkinPreviewList.Items.Add(skin);
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

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ReloadSkinPacks());
        }

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
                        string InstallationPath = FilepathManager.Default.GetInstallationsFolderPath(ConfigManager.Default.CurrentProfile, ConfigManager.Default.CurrentInstallation.DirectoryName);
                        string NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        string NewPackDirectory = Path.Combine(FilepathManager.Default.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);

                        while (Directory.Exists(NewPackDirectory))
                        {
                            NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                            NewPackDirectory = Path.Combine(FilepathManager.Default.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);
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

        private void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigManager.Default.OnConfigStateChanged(sender, Events.ConfigStateArgs.Empty);
        }
    }
}
