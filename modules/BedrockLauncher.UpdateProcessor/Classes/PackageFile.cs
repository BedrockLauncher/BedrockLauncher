using System;
using System.Collections.Generic;

// Credit to https://github.com/LukeFZ/MsixvcPackageDownloader

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public class PackageFile : BasePackageFile
    {
        public Guid ContentId { get; set; }
        public string VersionId { get; set; }
        public string FileName { get; set; }
        public ulong FileSize { get; set; }
        public string FileHash { get; set; }
        public string KeyBlob { get; set; }
        public List<string> CdnRootPaths { get; set; }
        public List<string> BackgroundCdnRootPaths { get; set; }
        public uint UpdateType { get; set; } // TODO: I think this is an enum - find out all values
        public Guid? DeltaVersionId { get; set; }
        public uint LicenseUsageType { get; set; }
        public ulong Clock { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
