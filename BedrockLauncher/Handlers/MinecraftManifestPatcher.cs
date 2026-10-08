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

        private static readonly XNamespace Desktop6 =
            "http://schemas.microsoft.com/appx/manifest/desktop/windows10/6";

        private const string CustomInstallCategory = "windows.customInstall";

        private static readonly string[] RemovedCapabilities =
        {
            "customInstallActions",
            "runFullTrust"
        };

        /// <summary>
        /// Normalizes a Minecraft package manifest for loose registration. Deterministic and idempotent: applying it
        /// again to its own output changes nothing. Identity-safe: the manifest must already be the expected package
        /// (identity name and publisher, compared case-insensitively); only the letter case of the name is normalized,
        /// so a release manifest can never be turned into a preview one or vice versa.
        /// Capabilities are preserved except customInstallActions and runFullTrust; the windows.customInstall
        /// extension that requires customInstallActions is removed with it so the manifest stays valid.
        /// </summary>
        /// <returns>True when the document was modified.</returns>
        internal static bool Apply(
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

            string manifestName = (string)identity.Attribute("Name");
            string manifestPublisher = (string)identity.Attribute("Publisher");

            if (!string.Equals(manifestName, identityName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"The package manifest identity is '{manifestName}', not '{identityName}'.");
            }

            if (!string.Equals(manifestPublisher?.Trim(), publisher?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"The package manifest publisher is '{manifestPublisher}', not '{publisher}'.");
            }

            string before = doc.ToString(SaveOptions.DisableFormatting);

            identity.SetAttributeValue("Name", identityName);

            foreach (var app in applications)
            {
                var executable = app.Attribute("Executable");

                if (executable != null &&
                    (executable.Value.Equals(
                        "GameLaunchHelper.exe",
                        StringComparison.OrdinalIgnoreCase) ||
                     executable.Value.Equals(
                        "GDKLaunchShim.exe",
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
                    .Where(element => RemovedCapabilities.Any(name =>
                        string.Equals(
                            element.Attribute("Name")?.Value,
                            name,
                            StringComparison.OrdinalIgnoreCase)))
                    .ToList())
                {
                    capability.Remove();
                }
            }

            foreach (var customInstall in root
                .Descendants(Desktop6 + "Extension")
                .Where(element => string.Equals(
                    (string)element.Attribute("Category"),
                    CustomInstallCategory,
                    StringComparison.OrdinalIgnoreCase))
                .ToList())
            {
                XElement container = customInstall.Parent;
                customInstall.Remove();

                if (container != null && container.Name == Foundation + "Extensions" && !container.HasElements)
                    container.Remove();
            }

            return !string.Equals(before, doc.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
        }
    }
}
