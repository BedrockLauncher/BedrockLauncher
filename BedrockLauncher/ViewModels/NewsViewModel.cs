using BedrockLauncher.Classes.Launcher;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.ViewModels
{
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class NewsViewModel
    {
        public static NewsViewModel Default { get; set; } = new NewsViewModel();


        public bool Launcher_ShowReleases { get; set; } = true;
        public bool Launcher_ShowBetas { get; set; } = true;

        public bool Official_ShowJavaContent { get; set; } = true;
        public bool Official_ShowDungeonsContent { get; set; } = true;
        public bool Official_ShowBedrockContent { get; set; } = true;
        public string Official_SearchBoxText { get; set; } = string.Empty;

        public ObservableCollection<News_OfficialItem> FeedItemsOfficial { get; set; } = new ObservableCollection<News_OfficialItem>();
        public ObservableCollection<PatchNote_Launcher> LauncherNewsItems { get; set; } = new ObservableCollection<PatchNote_Launcher>();

    }
}
