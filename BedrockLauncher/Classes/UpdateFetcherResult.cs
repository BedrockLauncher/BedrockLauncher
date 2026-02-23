using BedrockLauncher.Enums;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.ViewModels;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes
{
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class UpdateFetcherResult
    {
        public VersionFetcherState State { private get; set; }
        public string ThinkingVisibility => State == VersionFetcherState.Connecting ? "Visible" : "Collapsed";
        public bool IsDone => State == VersionFetcherState.Finished;

        public VersionInfoJson? LatestRelease { get; private set; }
        public VersionInfoJson? LatestPreview { get; private set; }

        public UpdateFetcherResult()
        {
            State = VersionFetcherState.Dormant;
            LatestRelease = null;
            LatestPreview = null;
        }

        public void RegisterVersions(IEnumerable<VersionInfoJson> versions)
        {
            foreach (VersionInfoJson version in versions)
            {
                switch (version.type)
                {
                    // currently only one version is able to be found. This could change in the future.
                    case UpdateProcessor.Enums.VersionType.Release:
                        LatestRelease = version;
                        break;
                    case UpdateProcessor.Enums.VersionType.Preview:
                        LatestPreview = version;
                        break;
                    default:
                        Trace.TraceWarning($"Could not categorize update {version.version} of type {version.type}.");
                        break;
                }
            }
        }

        public static void Reset()
        {
            MainDataModel.Default.FetcherResult = new UpdateFetcherResult();
        }
    }
}
