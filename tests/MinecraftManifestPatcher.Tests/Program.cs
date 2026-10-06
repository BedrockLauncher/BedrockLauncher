using System.Xml.Linq;
using BedrockLauncher.Handlers;

const string foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
const string uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
const string desktop = "http://schemas.microsoft.com/appx/manifest/desktop/windows10";
const string rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";

XNamespace ns = foundation;
XNamespace uapNs = uap;
XNamespace rescapNs = rescap;

var manifest = XDocument.Parse($"""
    <Package xmlns="{foundation}" xmlns:uap="{uap}" xmlns:desktop="{desktop}" xmlns:rescap="{rescap}" IgnorableNamespaces="uap desktop">
      <Identity Name="Original.Game" Publisher="CN=Original" Version="1.0.0.0" />
      <Applications>
        <Application Id="App" Executable="GameLaunchHelper.exe" EntryPoint="Old.EntryPoint">
          <uap:VisualElements DisplayName="Minecraft" />
          <uap:Extensions><uap:Extension Category="example" /></uap:Extensions>
          <desktop:Extensions><desktop:Extension Category="windows.fullTrustProcess" /></desktop:Extensions>
        </Application>
      </Applications>
      <Extensions><Extension Category="root-extension" /></Extensions>
      <Capabilities><Capability Name="internetClient" /></Capabilities>
    </Package>
    """);

MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", "CN=Microsoft Corporation");
MinecraftManifestPatcher.Apply(manifest, "Microsoft.MinecraftUWP", "CN=Microsoft Corporation");
manifest = XDocument.Parse(manifest.ToString(SaveOptions.DisableFormatting));

var root = manifest.Root ?? throw new Exception("root was removed");
var app = root.Descendants(ns + "Application").Single();
var capabilities = root.Element(ns + "Capabilities") ?? throw new Exception("capabilities missing");

Require((string?)root.Element(ns + "Identity")?.Attribute("Name") == "Microsoft.MinecraftUWP", "identity name not patched");
Require((string?)app.Attribute("Executable") == "Minecraft.Windows.exe", "launch shim not patched");
Require((string?)app.Attribute("EntryPoint") == "Minecraft_Win10.App", "Minecraft_Win10.App entry point missing");
Require((string?)app.Attribute("EntryPoint") != "Windows.FullTrustApplication", "Windows.FullTrustApplication must not be used");
Require(root.Descendants(uapNs + "VisualElements").Any(), "non-extension uap metadata was removed");
Require(capabilities.Elements(ns + "Capability").Any(element => (string?)element.Attribute("Name") == "internetClient"),
    "existing capabilities were not preserved");
Require(!capabilities.Elements(rescapNs + "Capability").Any(element => (string?)element.Attribute("Name") == "runFullTrust"),
    "runFullTrust must be removed");
Require(PackageRegistrationMatcher.SameFamily("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8WEKYB3D8BBWE"),
    "family matching should be case-insensitive");
Require(!PackageRegistrationMatcher.SameFamily("Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"),
    "release and preview families were incorrectly treated as equal");
Require(PackageRegistrationMatcher.SameVersion("1.21.114.1", "1.21.114.1"), "matching package version was rejected");
Require(!PackageRegistrationMatcher.SameVersion("1.21.114.1", "1.26.5004.0"), "different package version was accepted");
Require(!PackageRegistrationMatcher.MatchesRegistration(
    "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
    sameInstallDirectory: false, signedPackageRegistration: false,
    expectedVersion: "1.21.114.1", installedVersion: "1.21.114.1"),
    "a Store install with the same version was mistaken for the selected loose package");
Require(PackageRegistrationMatcher.MatchesRegistration(
    "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
    sameInstallDirectory: false, signedPackageRegistration: true,
    expectedVersion: "1.21.114.1", installedVersion: "1.21.114.1"),
    "the selected signed GDK package was not recognized by family and version");
Require(!PackageRegistrationMatcher.MatchesRegistration(
    "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
    sameInstallDirectory: false, signedPackageRegistration: true,
    expectedVersion: "1.21.114.1", installedVersion: "1.26.5004.0"),
    "a different signed package version was mistaken for the selected version");

Console.WriteLine("Minecraft manifest and registration matching tests passed.");

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
