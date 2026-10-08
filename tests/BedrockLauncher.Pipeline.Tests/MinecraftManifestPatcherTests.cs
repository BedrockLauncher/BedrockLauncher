using System.Xml.Linq;
using BedrockLauncher.Handlers;
using Xunit;

namespace BedrockLauncher.Pipeline.Tests
{
    /// <summary>Ported and extended from the former tests/MinecraftManifestPatcher.Tests console program.</summary>
    public class MinecraftManifestPatcherTests
    {
        private const string Foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        private const string Uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        private const string Desktop = "http://schemas.microsoft.com/appx/manifest/desktop/windows10";
        private const string Desktop6 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/6";
        private const string Rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
        private const string MicrosoftPublisher = "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US";

        private static readonly XNamespace Ns = Foundation;
        private static readonly XNamespace RescapNs = Rescap;

        private static XDocument GdkManifest(string identityName = "MICROSOFT.MINECRAFTUWP", string executable = "GameLaunchHelper.exe") => XDocument.Parse($"""
            <Package xmlns="{Foundation}" xmlns:uap="{Uap}" xmlns:desktop="{Desktop}" xmlns:desktop6="{Desktop6}" xmlns:rescap="{Rescap}" IgnorableNamespaces="uap desktop desktop6 rescap">
              <Identity Name="{identityName}" Publisher="{MicrosoftPublisher}" Version="1.21.12004.0" ProcessorArchitecture="x64" />
              <Applications>
                <Application Id="Game" Executable="{executable}" EntryPoint="Windows.FullTrustApplication">
                  <uap:VisualElements DisplayName="Minecraft for Windows" />
                  <Extensions><uap:Extension Category="windows.protocol"><uap:Protocol Name="minecraft" /></uap:Extension></Extensions>
                </Application>
              </Applications>
              <Extensions>
                <desktop6:Extension Category="windows.customInstall">
                  <desktop6:CustomInstall Folder="Installers" />
                </desktop6:Extension>
              </Extensions>
              <Capabilities>
                <Capability Name="internetClient" />
                <rescap:Capability Name="runFullTrust" />
                <rescap:Capability Name="appLicensing" />
                <rescap:Capability Name="unvirtualizedResources" />
                <rescap:Capability Name="customInstallActions" />
              </Capabilities>
            </Package>
            """);

        [Fact]
        public void LaunchShimIsReplacedAndEntryPointIsMinecraftWin10App()
        {
            XDocument manifest = GdkManifest();

            MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher);

            XElement app = manifest.Descendants(Ns + "Application").Single();
            Assert.Equal("Minecraft.Windows.exe", (string)app.Attribute("Executable"));
            Assert.Equal("Minecraft_Win10.App", (string)app.Attribute("EntryPoint"));
            Assert.Equal("Microsoft.MinecraftUWP", (string)manifest.Root.Element(Ns + "Identity").Attribute("Name"));
        }

        [Fact]
        public void GdkLaunchShimNameIsAlsoReplaced()
        {
            XDocument manifest = GdkManifest(executable: "GDKLaunchShim.exe");

            MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher);

            Assert.Equal("Minecraft.Windows.exe", (string)manifest.Descendants(Ns + "Application").Single().Attribute("Executable"));
        }

        [Fact]
        public void PatchingIsIdempotent()
        {
            XDocument manifest = GdkManifest();

            Assert.True(MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher));
            string once = manifest.ToString(SaveOptions.DisableFormatting);

            Assert.False(MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher));
            Assert.Equal(once, manifest.ToString(SaveOptions.DisableFormatting));

            // Also stable across a serialize/parse round trip.
            XDocument reparsed = XDocument.Parse(once);
            Assert.False(MinecraftManifestPatcher.Apply(reparsed, "Microsoft.MinecraftUWP", MicrosoftPublisher));
        }

        [Fact]
        public void ExistingCapabilitiesArePreservedAndNotDuplicated()
        {
            XDocument manifest = GdkManifest();

            MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher);
            MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher);

            List<string> names = manifest.Root.Element(Ns + "Capabilities").Elements()
                .Select(x => (string)x.Attribute("Name"))
                .ToList();

            Assert.Equal(new[] { "internetClient", "appLicensing", "unvirtualizedResources" }, names);
            Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.DoesNotContain(manifest.Root.Element(Ns + "Capabilities").Elements(RescapNs + "Capability"),
                x => (string)x.Attribute("Name") == "runFullTrust");
        }

        [Fact]
        public void CustomInstallExtensionIsRemovedWithItsCapabilitySoTheManifestStaysValid()
        {
            XDocument manifest = GdkManifest();

            MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher);

            Assert.DoesNotContain(manifest.Descendants(), x => (string)x.Attribute("Category") == "windows.customInstall");
            Assert.Null(manifest.Root.Element(Ns + "Extensions"));
            // The application's own extensions are untouched.
            Assert.Contains(manifest.Descendants(), x => (string)x.Attribute("Category") == "windows.protocol");
        }

        [Fact]
        public void UwpManifestIsLeftUnchanged()
        {
            XDocument manifest = XDocument.Parse($"""
                <Package xmlns="{Foundation}" xmlns:uap="{Uap}">
                  <Identity Name="Microsoft.MinecraftUWP" Publisher="{MicrosoftPublisher}" Version="1.21.3003.0" ProcessorArchitecture="x64" />
                  <Applications><Application Id="App" Executable="Minecraft.Windows.exe" EntryPoint="Minecraft_Win10.App" /></Applications>
                  <Capabilities><Capability Name="internetClientServer" /><Capability Name="privateNetworkClientServer" /></Capabilities>
                </Package>
                """);
            string before = manifest.ToString(SaveOptions.DisableFormatting);

            Assert.False(MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", MicrosoftPublisher));
            Assert.Equal(before, manifest.ToString(SaveOptions.DisableFormatting));
        }

        [Theory]
        [InlineData("Microsoft.MinecraftWindowsBeta", "Microsoft.MinecraftUWP")]
        [InlineData("Microsoft.MinecraftUWP", "Microsoft.MinecraftWindowsBeta")]
        [InlineData("Original.Game", "Microsoft.MinecraftUWP")]
        public void IdentityIsNeverRewrittenToAnotherPackage(string manifestIdentity, string expectedIdentity)
        {
            XDocument manifest = GdkManifest(identityName: manifestIdentity);
            string before = manifest.ToString(SaveOptions.DisableFormatting);

            Assert.Throws<InvalidDataException>(() => MinecraftManifestPatcher.Apply(manifest, expectedIdentity, MicrosoftPublisher));
            Assert.Equal(before, manifest.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void PublisherMustMatch()
        {
            XDocument manifest = GdkManifest();

            Assert.Throws<InvalidDataException>(() => MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", "CN=Someone Else"));
        }
    }
}
