// Credit to https://github.com/LukeFZ/MsixvcPackageDownloader

namespace BedrockLauncher.UpdateProcessor.Classes
{    public class PackageMetadataFile : BasePackageFile
    {
        public string Name { get; set; }
        public ulong Size { get; set; }
        public string License { get; set; }
    }
}
