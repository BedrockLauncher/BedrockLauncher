using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher
{
    public static class Constants
    {
        public static readonly string MINECRAFT_EXE_NAME = "Minecraft.Windows";
        public static readonly string MINECRAFT_PACKAGE_FAMILY = "Microsoft.MinecraftUWP_8wekyb3d8bbwe";

        public static readonly string LATEST_BETA_UUID = "latest_beta";
        public static readonly string LATEST_RELEASE_UUID = "latest_release";

        public static class Debugging
        {
            public const bool ExcludeExplicitProperties = false;
            public static bool CalculateVersionSizes { get; internal set; } = false;
            public static bool UpdateVersionsOnLoad { get; internal set; } = true;
            public static bool CheckForUpdatesOnLoad { get; internal set; } = true;


            public static void ThrowIntentionalException()
            {
                Exception ex = new Exception("Intentionally Thrown Exception by Developer");
                throw ex;
            }
        }
    }



}
