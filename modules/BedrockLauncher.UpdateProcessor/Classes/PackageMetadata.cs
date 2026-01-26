using System.Collections.Generic;

// Credit to https://github.com/LukeFZ/MsixvcPackageDownloader

namespace BedrockLauncher.UpdateProcessor.Classes
{    public class PackageMetadata
    {
        public List<string> CdnRoots { get; set; }
        public List<string> BackgroundCdnRootPaths { get; set; }
        public List<PackageMetadataFile> Files { get; set; }
        public ulong EstimatedTotalDownloadSize { get; set; }
    }
}
