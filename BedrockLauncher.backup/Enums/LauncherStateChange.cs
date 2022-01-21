﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Enums
{
    public enum LauncherStateChange
    {
        None,
        isInitializing,
        isExtracting,
        isUninstalling,
        isLaunching,
        isDownloading,
        isBackingUp,
        isRemovingPackage,
        isRegisteringPackage
    }
}
