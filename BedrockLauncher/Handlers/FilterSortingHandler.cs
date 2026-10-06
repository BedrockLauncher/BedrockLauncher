using BedrockLauncher.Classes;
using BedrockLauncher.Classes.Launcher;
using BedrockLauncher.Enums;
using BedrockLauncher.UpdateProcessor.Enums;
using BedrockLauncher.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace BedrockLauncher.Handlers
{
    public static class FilterSortingHandler
    {
        public static string InstallationsSearchFilter { get; set; } = string.Empty;

        public static SortDescription? GetInstallationSortDescriptor()
        {
            return Properties.LauncherSettings.Default.InstallationsSortMode switch
            {
                InstallationSort.LatestPlayed =>
                    new SortDescription(
                        nameof(BLInstallation.LastPlayedT),
                        ListSortDirection.Descending),

                InstallationSort.Name =>
                    new SortDescription(
                        nameof(BLInstallation.DisplayName),
                        ListSortDirection.Ascending),

                InstallationSort.None => null,

                _ =>
                    new SortDescription(
                        nameof(BLInstallation.LastPlayedT),
                        ListSortDirection.Descending)
            };
        }

        public static void Refresh(object itemSource)
        {
            if (itemSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(itemSource);

            view?.Refresh();
        }

        public static void Sort_InstallationList(object itemSource)
        {
            if (itemSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(itemSource);

            if (view == null)
                return;

            view.SortDescriptions.Clear();

            var sortDescriptor = GetInstallationSortDescriptor();

            if (sortDescriptor.HasValue)
                view.SortDescriptions.Add(sortDescriptor.Value);

            view.Refresh();
        }

        public static bool Filter_InstallationList(object obj)
        {
            if (obj is not BLInstallation installation)
                return false;

            var settings = Properties.LauncherSettings.Default;

            if (!settings.ShowPreviews && installation.IsPreview)
                return false;

            if (!settings.ShowBetas && installation.IsBeta)
                return false;

            if (!settings.ShowReleases && installation.IsRelease)
                return false;

            if (!string.IsNullOrEmpty(InstallationsSearchFilter) &&
                (installation.DisplayName == null ||
                 !installation.DisplayName.Contains(
                     InstallationsSearchFilter,
                     StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        public static bool Filter_VersionList(object obj)
        {
            if (obj is not MCVersion version || !version.IsInstalled)
                return false;

            var settings = Properties.LauncherSettings.Default;

            if (version.PackageType == PackageType.UWP &&
                !settings.ShowUWP)
            {
                return false;
            }

            if (version.PackageType == PackageType.GDK &&
                !settings.ShowGDK)
            {
                return false;
            }

            if (version.IsPreview)
                return settings.ShowPreviews;

            if (version.IsBeta)
                return settings.ShowBetas;

            if (version.IsRelease)
                return settings.ShowReleases;

            return false;
        }

        public static bool Filter_OfficialNewsFeed(object obj)
        {
            if (obj is not News_OfficialItem item)
                return false;

            if (item.newsType == null ||
                !item.newsType.Any(nt => !string.IsNullOrEmpty(nt) && nt.Contains("News page", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var viewModel = NewsViewModel.Default;

            bool categoryEnabled = item.category switch
            {
                "Minecraft: Java Edition" =>
                    viewModel.Official_ShowJavaContent,

                "Minecraft Dungeons" =>
                    viewModel.Official_ShowDungeonsContent,

                "Minecraft for Windows" =>
                    viewModel.Official_ShowBedrockContent,

                _ => false
            };

            if (!categoryEnabled)
                return false;

            string searchText = viewModel.Official_SearchBoxText;

            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return !string.IsNullOrEmpty(item.title) &&
                   item.title.Contains(
                       searchText,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
