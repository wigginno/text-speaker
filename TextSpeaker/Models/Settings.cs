using System.Text.Json.Serialization;

namespace TextSpeaker.Models
{
    public class Settings
    {
        public string? AzureSpeechKey { get; set; }
        public string? AzureSpeechRegion { get; set; }

        // Optional: Add other settings here if needed in the future
        // Example: public string DefaultOutputPath { get; set; }
    }

    // Add this partial class for System.Text.Json source generation
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}
