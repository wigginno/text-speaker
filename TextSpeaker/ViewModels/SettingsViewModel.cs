// TextSpeaker/ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TextSpeaker.Models;
using TextSpeaker.Services;

namespace TextSpeaker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? _azureSpeechKey;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? _azureSpeechRegion;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public event EventHandler? CloseRequested; // Event to signal window close

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadCurrentSettings();
            StatusMessage = $"Settings loaded from: {_settingsService.GetSettingsFilePath()}";
        }

        private void LoadCurrentSettings()
        {
            AzureSpeechKey = _settingsService.CurrentSettings.AzureSpeechKey;
            AzureSpeechRegion = _settingsService.CurrentSettings.AzureSpeechRegion;
        }

        private bool CanSave()
        {
            // Basic validation: ensure key/region are not just whitespace
            return !string.IsNullOrWhiteSpace(AzureSpeechKey) &&
                   !string.IsNullOrWhiteSpace(AzureSpeechRegion);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            StatusMessage = "Saving settings...";
            var settingsToSave = new Settings
            {
                AzureSpeechKey = this.AzureSpeechKey,
                AzureSpeechRegion = this.AzureSpeechRegion
            };

            try
            {
                await _settingsService.SaveSettingsAsync(settingsToSave);
                StatusMessage = "Settings saved successfully. Close this window for changes to take effect.";
                // Optionally, disable save button after successful save until changes are made
                SaveCommand.NotifyCanExecuteChanged(); // Re-evaluates CanSave
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty); // Raise the event
        }
    }
}
