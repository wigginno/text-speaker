// TextSpeaker/Services/SettingsService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks; // Required for Task
using TextSpeaker.Models;

namespace TextSpeaker.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        // Removed _jsonOptions as we'll use source generation context
        private Settings _currentSettings; // Backing field for CurrentSettings

        // Implementation of the CurrentSettings property from the interface
        public Settings CurrentSettings => _currentSettings;

        public SettingsService(string settingsFilePath)
        {
            _settingsFilePath = settingsFilePath;
            // Removed _jsonOptions initialization

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Load settings synchronously on initialization to ensure CurrentSettings is populated.
            // The async version is available for explicit reloads if needed elsewhere.
            _currentSettings = LoadSettingsInternal();
        }

        // Internal synchronous load method for constructor initialization
        private Settings LoadSettingsInternal()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    // Handle potential empty or whitespace file
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new Settings(); // Return default if file is empty
                    }
                    // Use source-generated context for deserialization
                    var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);
                    return settings ?? new Settings(); // Return default if deserialization yields null
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error reading or parsing settings file '{_settingsFilePath}': {jsonEx.Message}. Returning default settings.");
                // Optionally: backup the corrupted file
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"Error accessing settings file '{_settingsFilePath}': {ioEx.Message}. Returning default settings.");
            }
            catch (Exception ex) // Catch any other unexpected errors during load
            {
                Console.WriteLine($"Unexpected error loading settings from '{_settingsFilePath}': {ex.Message}. Returning default settings.");
            }

            return new Settings(); // Return default settings if file doesn't exist or error occurs
        }

        // Implementation of LoadSettingsAsync from the interface
        public async Task LoadSettingsAsync()
        {
            try
            {
                Settings loadedSettings;
                if (File.Exists(_settingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(_settingsFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        loadedSettings = new Settings();
                    }
                    else
                    {
                        // Use source-generated context for deserialization
                        loadedSettings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings) ?? new Settings();
                    }
                }
                else
                {
                    loadedSettings = new Settings(); // File doesn't exist, use defaults
                }
                // Update the backing field
                _currentSettings = loadedSettings;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error reading or parsing settings file '{_settingsFilePath}' during async load: {jsonEx.Message}. Keeping existing settings.");
                // Keep _currentSettings as is if async load fails
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"Error accessing settings file '{_settingsFilePath}' during async load: {ioEx.Message}. Keeping existing settings.");
                // Keep _currentSettings as is if async load fails
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Unexpected error during async load from '{_settingsFilePath}': {ex.Message}. Keeping existing settings.");
                 // Keep _currentSettings as is if async load fails
            }
            // No return value needed as it updates the CurrentSettings property implicitly
        }

        // Implementation of SaveSettingsAsync from the interface
        public async Task SaveSettingsAsync(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings); // Ensure settings object is not null

            try
            {
                // Use source-generated context for serialization
                string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
                await File.WriteAllTextAsync(_settingsFilePath, json);

                // Update the current settings in memory after successful save
                _currentSettings = settings;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error serializing settings: {jsonEx.Message}");
                // Consider re-throwing, logging more severely, or notifying the user
                throw; // Re-throw to indicate the save failed
            }
            catch (IOException ioEx)
            {
                 Console.WriteLine($"Error writing settings file '{_settingsFilePath}': {ioEx.Message}");
                 // Consider re-throwing, logging more severely, or notifying the user
                 throw; // Re-throw to indicate the save failed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error saving settings to '{_settingsFilePath}': {ex.Message}");
                // Consider re-throwing, logging more severely, or notifying the user
                throw; // Re-throw to indicate the save failed
            }
        }

        // Implementation of GetSettingsFilePath from the interface
        public string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }
    }
}
