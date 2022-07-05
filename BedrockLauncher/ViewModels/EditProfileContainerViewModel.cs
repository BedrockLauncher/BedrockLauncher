using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.ViewModels
{
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class EditProfileContainerViewModel
    {
        public string ProfileName { get; set; } = string.Empty;
        public string ProfileDirectory { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public string ProfileUUID { get; set; } = Guid.NewGuid().ToString();
    }
}
