namespace BedrockLauncher.Localization.Properties
{
    public static class Settings
    {
        public static SettingsDefault Default { get; } = new SettingsDefault();
    }

    public class SettingsDefault
    {
        public string Language { get; set; } = "en-US";
        public void Save() { }
    }
}
