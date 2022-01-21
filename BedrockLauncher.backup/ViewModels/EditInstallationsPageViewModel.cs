using PostSharp.Patterns.Model;

namespace BedrockLauncher.ViewModels
{
    /// <summary>
    /// Interaction logic for EditInstallationScreen.xaml
    /// </summary>
    /// 

    [NotifyPropertyChanged(ExcludeExplicitProperties=Constants.Debugging.ExcludeExplicitProperties)]
    public class EditInstallationsPageViewModel
    {
        public string SelectedVersionUUID { get; set; } = string.Empty;
        public string SelectedUUID { get; set; } = string.Empty;
        public string InstallationName { get; set; } = string.Empty;
        public string InstallationDirectory { get; set; } = string.Empty;
    }
}
