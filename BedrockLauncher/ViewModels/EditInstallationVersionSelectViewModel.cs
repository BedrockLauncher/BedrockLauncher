using PostSharp.Patterns.Model;
using System;

namespace BedrockLauncher.ViewModels
{
    /// <summary>
    /// Interaction logic for EditInstallationScreen.xaml
    /// </summary>
    /// 

    [NotifyPropertyChanged(ExcludeExplicitProperties=Constants.Debugging.ExcludeExplicitProperties)]
    public class EditInstallationVersionSelectViewModel
    {
        public string FilterString { get; set; } = string.Empty;

        public bool ShowRelease { get; set; } = true;
        public bool ShowBeta { get; set; } = true;
        public bool ShowPreview { get; set; } = true;
        public bool ShowImported { get; set; } = true;

        public bool ShowX86 { get; set; } = true;
        public bool ShowX64 { get; set; } = true;
        public bool ShowARM { get; set; } = true;

        internal void Update()
        {
            throw new NotImplementedException();
        }
    }
}
