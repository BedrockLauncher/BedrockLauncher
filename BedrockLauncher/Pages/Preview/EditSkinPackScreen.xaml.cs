using BedrockLauncher.Classes.SkinPack;
using BedrockLauncher.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using System.Net.Cache;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.Preview
{
    /// <summary>
    /// Interaction logic for EditSkinPackScreen.xaml
    /// </summary>
    public partial class EditSkinPackScreen : Page, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public MCSkinPack CurrentSkinPack;

        private int Index = -1;

        private bool isEditMode = false;

        #region SkinPack Accessors

        public string LocalizationName
        {
            get
            {
                return CurrentSkinPack.Content.localization_name;
            }
            set
            {
                CurrentSkinPack.Content.localization_name = value;
                OnPropertyChanged("LocalizationName");
            }
        }
        public string SerializableName
        {
            get
            {
                return CurrentSkinPack.Content.serialize_name;
            }
            set
            {
                CurrentSkinPack.Content.serialize_name = value;
                OnPropertyChanged("SerializableName");
            }
        }
        public string GeometryPath
        {
            get
            {
                return CurrentSkinPack.Content.geometry;
            }
            set
            {
                CurrentSkinPack.Content.geometry = value;
                OnPropertyChanged("GeometryPath");
            }
        }
        
        public int FormatVersion
        {
            get
            {
                return CurrentSkinPack.Metadata.format_version;
            }
            set
            {
                CurrentSkinPack.Metadata.format_version = value;
                OnPropertyChanged("FormatVersion");
            }
        }

        public string HeaderName
        {
            get
            {
                return CurrentSkinPack.Metadata.header.name;
            }
            set
            {
                CurrentSkinPack.Metadata.header.name = value;
                OnPropertyChanged(nameof(HeaderName));
            }
        }
        public string HeaderDescription
        {
            get
            {
                return CurrentSkinPack.Metadata.header.description;
            }
            set
            {
                CurrentSkinPack.Metadata.header.description = value;
                OnPropertyChanged(nameof(HeaderDescription));
            }
        }
        public string HeaderUUID
        {
            get
            {
                return CurrentSkinPack.Metadata.header.uuid;
            }
            set
            {
                CurrentSkinPack.Metadata.header.uuid = value;
                OnPropertyChanged("HeaderUUID");
            }
        }
        public int HeaderVersionMajor
        {
            get
            {
                return CurrentSkinPack.Metadata.header.version[0];
            }
            set
            {
                CurrentSkinPack.Metadata.header.version[0] = value;
                OnPropertyChanged("HeaderVersionMajor");
            }
        }
        public int HeaderVersionMinor
        {
            get
            {
                return CurrentSkinPack.Metadata.header.version[1];
            }
            set
            {
                CurrentSkinPack.Metadata.header.version[1] = value;
                OnPropertyChanged("HeaderVersionMinor");
            }
        }
        public int HeaderVersionRevision
        {
            get
            {
                return CurrentSkinPack.Metadata.header.version[2];
            }
            set
            {
                CurrentSkinPack.Metadata.header.version[2] = value;
                OnPropertyChanged("HeaderVersionRevision");
            }
        }

        public string ModuleType
        {
            get
            {
                return CurrentSkinPack.Metadata.modules.FirstOrDefault().type;
            }
            set
            {
                CurrentSkinPack.Metadata.modules.FirstOrDefault().type = value;
                OnPropertyChanged("ModuleType");
            }
        }
        public string ModuleUUID
        {
            get
            {
                return CurrentSkinPack.Metadata.modules.FirstOrDefault().uuid;
            }
            set
            {
                CurrentSkinPack.Metadata.modules.FirstOrDefault().uuid = value;
                OnPropertyChanged("ModuleUUID");
            }
        }
        public int ModuleVersionMajor
        {
            get
            {
                return CurrentSkinPack.Metadata.modules.FirstOrDefault().version[0];
            }
            set
            {
                CurrentSkinPack.Metadata.modules.FirstOrDefault().version[0] = value;
                OnPropertyChanged("ModuleVersionMajor");
            }
        }
        public int ModuleVersionMinor
        {
            get
            {
                return CurrentSkinPack.Metadata.modules.FirstOrDefault().version[1];
            }
            set
            {
                CurrentSkinPack.Metadata.modules.FirstOrDefault().version[1] = value;
                OnPropertyChanged("ModuleVersionMinor");
            }
        }
        public int ModuleVersionRevision
        {
            get
            {
                return CurrentSkinPack.Metadata.modules.FirstOrDefault().version[2];
            }
            set
            {
                CurrentSkinPack.Metadata.modules.FirstOrDefault().version[2] = value;
                OnPropertyChanged("ModuleVersionRevision");
            }
        }

        #endregion

        #region Init

        private string GetNewPackFolder()
        {
            string InstallationPath = MainViewModel.Default.FilePaths.GetInstallationsFolderPath(MainViewModel.Default.Config.CurrentProfileUUID, MainViewModel.Default.Config.CurrentInstallation.DirectoryName);
            string NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            string NewPackDirectory = Path.Combine(MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, MainViewModel.Default.Config.CurrentInstallation.VersionType), NewPackDirectoryName);

            while (Directory.Exists(NewPackDirectory))
            {
                NewPackDirectoryName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                NewPackDirectory = Path.Combine(MainViewModel.Default.FilePaths.GetSkinPacksFolderPath(InstallationPath, MainViewModel.Default.Config.CurrentInstallation.VersionType), NewPackDirectoryName);
            }

            return NewPackDirectory;
        }

        public EditSkinPackScreen()
        {
            CurrentSkinPack = new MCSkinPack(GetNewPackFolder());
            DataContext = this;

            InitializeComponent();
            Init(false);
        }


        public EditSkinPackScreen(MCSkinPack _skinPack, int _index)
        {
            CurrentSkinPack = _skinPack;
            DataContext = this;
            Index = _index;

            InitializeComponent();
            Init(true);
            InitEditFeilds();
        }

        private void Init(bool _isEditMode = true)
        {
            isEditMode = _isEditMode;
            if (isEditMode)
            {
                Header.SetResourceReference(TextBlock.TextProperty, "EditSkinPackScreen_Header");
                CreateButton.SetResourceReference(Button.ContentProperty, "GeneralText_Save");
            }
            else
            {
                Header.SetResourceReference(TextBlock.TextProperty, "EditSkinPackScreen_AddHeader");
                CreateButton.SetResourceReference(Button.ContentProperty, "GeneralText_Add");
            }
        }

        public void UpdateLocalizationList()
        {
            LocalizationList.Items.Clear();
            foreach (var entry in CurrentSkinPack.Texts.LangFiles)
            {
                LocalizationList.Items.Add(entry);
            }
        }

        private void UpdateIconImage()
        {

            var icon = CurrentSkinPack.CurrentIcon;

            if (icon.IsAbsoluteUri)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = icon;
                bmp.EndInit();

                IconPreview.Source = bmp;
            }
            else
            {
                IconPreview.Source = new BitmapImage(icon);
            }



        }

        private void InitEditFeilds()
        {
            UpdateIconImage();
            UpdateLocalizationList();
        }

        #endregion

        #region Closing Events

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode) RemoveUnfinishedPack();
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSkinPack();
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        #endregion

        #region Saving

        public void RemoveUnfinishedPack()
        {
            Directory.Delete(CurrentSkinPack.Directory, true);
        }

        public void SaveSkinPack()
        {
            CurrentSkinPack.Save();
        }

        #endregion


        public string ValidateLangFile(string lang_name, bool createFile = true)
        {
            string filePath = Path.Combine(CurrentSkinPack.Directory, "texts", string.Format("{0}.lang", lang_name));
            if (!File.Exists(filePath) && createFile)
            {
                try
                {
                    File.Create(filePath).Close();
                    CurrentSkinPack.Texts.AddLang(lang_name);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }

            }
            return filePath;
        }

        private void AddLangButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateLangFile(LocalizationAddTextBox.Text, true);
            UpdateLocalizationList();
        }
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "PNG Files (*.png)|*.png"
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToImport = ofd.FileName;
                File.Copy(fileToImport, CurrentSkinPack.IconPath);
                UpdateIconImage();
            }
        }

        private void ModuleUUIDRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleUUID = Guid.NewGuid().ToString();
        }
        private void HeaderUUIDRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            HeaderUUID = Guid.NewGuid().ToString();
        }
    }
}
