
using BedrockLauncher.Classes;
using BedrockLauncher.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BedrockLauncher.Handlers
{
    public static class FilterSortingHandler
    {
        public static string Installations_SearchFilter { get; set; } = string.Empty;
        public static SortBy_Installation Installations_SortFilter { get; set; } = SortBy_Installation.LatestPlayed;

        public static bool Filter_InstallationList(object obj)
        {
            BLInstallation v = obj as BLInstallation;
            if (v == null) return false;
            else if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
            else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
            else if (!v.DisplayName.Contains(Installations_SearchFilter)) return false;
            else return true;
        }
        public static void Sort_InstallationList(ref CollectionView view)
        {
            view.SortDescriptions.Clear();
            if (Installations_SortFilter == SortBy_Installation.LatestPlayed) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("LastPlayedT", System.ComponentModel.ListSortDirection.Descending));
            if (Installations_SortFilter == SortBy_Installation.Name) view.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayName", System.ComponentModel.ListSortDirection.Ascending));
        }
        public static bool Filter_VersionList(object obj)
        {
            BLVersion v = BLVersion.Convert(obj as MCVersion);

            if (v != null && v.IsInstalled)
            {
                if (!Properties.LauncherSettings.Default.ShowBetas && v.IsBeta) return false;
                else if (!Properties.LauncherSettings.Default.ShowReleases && !v.IsBeta) return false;
                else return true;
            }
            else return false;

        }
    }
}
