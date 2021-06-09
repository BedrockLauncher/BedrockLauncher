using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;


namespace BL_Setup.Pages
{
    /// <summary>
    /// Логика взаимодействия для WelcomePage.xaml
    /// </summary>
    public partial class InstallLocationPage : Page
    {

        private bool IsInit = false;

        public InstallLocationPage()
        {
            InitializeComponent();
            registerIconCheckBox.IsChecked = BL_Setup.MainWindow.Installer.RegisterAsProgram;
            desktopIconCheckBox.IsChecked = BL_Setup.MainWindow.Installer.MakeDesktopIcon;
            startMenuCheckBox.IsChecked = BL_Setup.MainWindow.Installer.MakeStartMenuIcon;
            IsInit = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            installPathTextBox.Text = Path.Combine(ProgramFilesx86(), "Minecraft Bedrock Launcher");
        }
        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowser = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = ProgramFilesx86();
                System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowser.SelectedPath))
                {
                    installPathTextBox.Text = Path.Combine(folderBrowser.SelectedPath, "Minecraft Bedrock Launcher");
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ((MainWindow) Application.Current.MainWindow).NextBtn.Content = "Install";
        }

        private void IconCheckBoxes_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (IsInit)
            {
                BL_Setup.MainWindow.Installer.MakeDesktopIcon = desktopIconCheckBox.IsChecked.Value;
                BL_Setup.MainWindow.Installer.MakeStartMenuIcon = startMenuCheckBox.IsChecked.Value;
                BL_Setup.MainWindow.Installer.RegisterAsProgram = registerIconCheckBox.IsChecked.Value;
            }
        }
    }
}
