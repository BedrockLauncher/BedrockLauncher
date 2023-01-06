using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using PostSharp.Patterns.Model;
using BedrockLauncher.ViewModels;
using JemExtensions;

namespace BedrockLauncher.Handlers
{
    public class FilterSortingHandler
    {
        public static InstallationSort InstallationsSortMode { get; set; } = InstallationSort.LatestPlayed;
        public static SortDescription GetInstallationSortDescriptor()
        {
            switch (InstallationsSortMode)
            {
                case InstallationSort.LatestPlayed:
                    return new SortDescription(nameof(BLInstallation.LastPlayedT), ListSortDirection.Descending);
                case InstallationSort.Name:
                    return new SortDescription(nameof(BLInstallation.DisplayName), ListSortDirection.Ascending);
                default:
                    return new SortDescription(nameof(BLInstallation.LastPlayedT), ListSortDirection.Descending);
            }
        }
        public static string InstallationsSearchFilter { get; set; } = string.Empty;

        public static void Refresh(object itemSource)
        {
            var view = CollectionViewSource.GetDefaultView(itemSource) as CollectionView;
            if (view != null) view.Refresh();
        }
        public static void Sort_InstallationList(object itemSource)
        {
            var view = CollectionViewSource.GetDefaultView(itemSource) as CollectionView;
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(GetInstallationSortDescriptor());
                view.Refresh();
            }
        }

        public static bool Filter_InstallationList(object obj)
        {
            BLInstallation v = obj as BLInstallation;
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!v.DisplayName.Contains(InstallationsSearchFilter)) return false;
            else return true;
        }
        public static bool Filter_VersionList(object obj)
        {
            MCVersion v = (obj as MCVersion);
            if (v != null && v.IsInstalled)
            {
                if (Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return true;
                else if (Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return true;
                else return false;
            }
            else return false;

        }

        public static bool Filter_OfficalNewsFeed(object obj)
        {
            if (!(obj is NewsItem_Offical)) return false;
            else
            {
                var item = (obj as NewsItem_Offical);
                if (item.newsType != null && item.newsType.Contains("News page"))
                {
                    if (item.category == "Minecraft: Java Edition" && NewsViewModel.Default.Offical_ShowJavaContent) return ContainsText(item);
                    else if (item.category == "Minecraft Dungeons" && NewsViewModel.Default.Offical_ShowDungeonsContent) return ContainsText(item);
                    else if (item.category == "Minecraft for Windows" && NewsViewModel.Default.Offical_ShowBedrockContent) return ContainsText(item);
                    else return false;
                }
                else return false;
            }

            bool ContainsText(NewsItem_Offical _item)
            {
                string searchParam = NewsViewModel.Default.Offical_SearchBoxText;
                if (string.IsNullOrEmpty(searchParam) || _item.title.Contains(searchParam, StringComparison.OrdinalIgnoreCase)) return true;
                else return false;
            }
        }
    }
}
