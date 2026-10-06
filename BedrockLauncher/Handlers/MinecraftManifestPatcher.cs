using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BedrockLauncher.Handlers
{
    internal static class MinecraftManifestPatcher
    {
        private static readonly XNamespace Foundation =
            "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

        private static readonly XNamespace RestrictedCapabilities =
            "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";

        internal static void Apply(
            XDocument doc,
            string identityName,
            string publisher)
        {
            var root = doc.Root
                ?? throw new InvalidDataException(
                    "The package manifest has no root element.");

            var identity = root.Element(Foundation + "Identity")
                ?? throw new InvalidDataException(
                    "The package manifest has no Identity element.");

            var applications = root
                .Descendants(Foundation + "Application")
                .ToList();

            if (applications.Count == 0)
            {
                throw new InvalidDataException(
                    "The package manifest has no Application element.");
            }

            identity.SetAttributeValue("Name", identityName);
            identity.SetAttributeValue("Publisher", publisher);

            foreach (var app in applications)
            {
                var executable = app.Attribute("Executable");

                if (executable != null &&
                    (executable.Value.Equals(
                        "GameLaunchHelper.exe",
                        StringComparison.OrdinalIgnoreCase) ||
                     executable.Value.Equals(
                        "GDKLaunchShim.exe",
                        StringComparison.OrdinalIgnoreCase) ||
                     executable.Value.Equals(
                        "gamelaunchhelper.exe",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    executable.Value = "Minecraft.Windows.exe";
                }

                app.SetAttributeValue(
                    "EntryPoint",
                    "Minecraft_Win10.App");
            }

            var capabilities = root.Element(
                Foundation + "Capabilities");

            if (capabilities != null)
            {
                foreach (var capability in capabilities
                    .Elements(RestrictedCapabilities + "Capability")
                    .Where(element =>
                        string.Equals(
                            element.Attribute("Name")?.Value,
                            "customInstallActions",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            element.Attribute("Name")?.Value,
                            "runFullTrust",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList())
                {
                    capability.Remove();
                }
            }
        }
    }
}
