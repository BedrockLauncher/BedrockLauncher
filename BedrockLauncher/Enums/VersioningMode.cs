using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Enums
{
    public enum VersioningMode : int
    {
        LatestPreview = 0,
        LatestBeta = 1,
        LatestRelease = 2,
        None = 3
    }
}
