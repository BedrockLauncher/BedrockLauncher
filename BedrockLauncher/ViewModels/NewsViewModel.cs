using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Controls.Items;
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

        public bool Offical_ShowJavaContent { get; set; } = true;
        public bool Offical_ShowDungeonsContent { get; set; } = true;
        public bool Offical_ShowBedrockContent { get; set; } = true;
        public string Offical_SearchBoxText { get; set; } = string.Empty;

        public ObservableCollection<NewsItem_Offical> FeedItemsOffical { get; set; } = new ObservableCollection<NewsItem_Offical>();
        public ObservableCollection<AppPatchNote> LauncherNewsItems { get; set; } = new ObservableCollection<AppPatchNote>();

    }
}
