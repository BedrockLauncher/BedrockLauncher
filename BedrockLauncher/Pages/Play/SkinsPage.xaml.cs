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
using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Extensions;
using System.IO;
using System.Data;
using BedrockLauncher.Controls.Items;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.IO.Compression;
using BedrockLauncher.Pages.Preview;
using BedrockLauncher.Pages.Common;
using Extensions;
using BedrockLauncher.Classes;
using BedrockLauncher.ViewModels;
using System.Collections.ObjectModel;
using PostSharp.Patterns.Model;
using BedrockLauncher.UI.Pages.Common;

namespace BedrockLauncher.Pages.Play
{
    /// <summary>
    /// Interaction logic for SkinsPage.xaml
    /// </summary>
    /// 
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class SkinsPageViewModel
    {
        public MCSkinPack CurrentSkinPack { get; set; }
        public MCSkin CurrentSkin { get; set; }
        public string CurrentSkinName
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkinPack != null && CurrentSkin != null ? CurrentSkinPack.GetLocalizedSkinName(CurrentSkin.localization_name) : "NULL";
            }
        }
        public string CurrentSkinPath
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkin != null ? CurrentSkin.texture_path : string.Empty;
            }
        }
        public MCSkinGeometry CurrentSkinType
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkin != null ? CurrentSkin.skin_type : MCSkinGeometry.Normal;
            }
        }

        public ObservableCollection<MCSkinPack> SkinPacks { get; set; } = new ObservableCollection<MCSkinPack>();
        public ObservableCollection<MCSkin> Skins
        {
            get
            {
                Depends.On(CurrentSkinPack);
                if (CurrentSkinPack != null) return CurrentSkinPack.Content.skins;
                else return new ObservableCollection<MCSkin>(); 
            }
        }
    }


    public partial class SkinsPage : Page
    {
        private bool HasLoadedOnce = false;

        public SkinsPageViewModel ViewModel { get; set; } = new SkinsPageViewModel();

        #region Init

        public SkinsPage()
        {
            this.DataContext = ViewModel;
            InitializeComponent();
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!HasLoadedOnce) HasLoadedOnce = true;
                ReloadSkinPacks();
            });
        }


        #endregion

        #region UI
        public void ReloadSkinPacks()
        {
            this.Dispatcher.Invoke(() =>
            {
                ViewModel.SkinPacks.Clear();

                var installation = MainViewModel.Default.Config.CurrentInstallation;

                if (installation == null) return;

                string InstallationPath = MainViewModel.Default.FilePaths.GetInstallationsFolderPath(MainViewModel.Default.Config.CurrentProfileUUID, installation.DirectoryName);
                string normal_folder = MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, false);
                string dev_folder = MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, true);

                if (Directory.Exists(normal_folder)) AddPacks(normal_folder);
                if (Directory.Exists(dev_folder)) AddPacks(dev_folder);

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
                                ViewModel.SkinPacks.Add(result);
                            }
                        }


                    }
                }
            });
        }
        #endregion

        #region Selections

        private void LoadedSkinPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void SkinPreviewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void InstallationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Task.Run(ReloadSkinPacks);
        }

        #endregion

        #region Clicks
        private void AddSkinButton_Click(object sender, RoutedEventArgs e)
        {
            var skinPack = LoadedSkinPacks.SelectedItem as MCSkinPack;
            ViewModels.MainViewModel.Default.SetOverlayFrame(new EditSkinScreen(skinPack));
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
                        string InstallationPath = MainViewModel.Default.FilePaths.GetInstallationsFolderPath(MainViewModel.Default.Config.CurrentProfileUUID, MainViewModel.Default.Config.CurrentInstallation.DirectoryName);
                        string NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        string NewPackDirectory = Path.Combine(MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);

                        while (Directory.Exists(NewPackDirectory))
                        {
                            NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                            NewPackDirectory = Path.Combine(MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, false), NewPackDirectoryName);
                        }

                        ZipFile.ExtractToDirectory(dialog.FileName, NewPackDirectory);
                    }
                    else
                    {
                        file.Dispose();
                        ErrorScreenShow.errormsg("Error_NotaSkinPack_Title", "Error_NotaSkinPack");
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
            ViewModels.MainViewModel.Default.SetOverlayFrame(new EditSkinPackScreen());
        }
        private void SkinPreviewList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            ListViewItem lbi = (SkinPreviewList.ItemContainerGenerator.ContainerFromIndex(SkinPreviewList.SelectedIndex)) as ListViewItem;
            ContentPresenter cp = WPFExtensions.FindVisualChild<ContentPresenter>(lbi);
            DataTemplate dt = lbi.ContentTemplate;
            SkinItem item = (dt.FindName("ItemControl", cp)) as SkinItem;

            if (item != null && SkinPreviewList.IsKeyboardFocusWithin) item.OpenContextMenu();
        }
        private void LoadedSkinPacks_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        #endregion
    }
}
