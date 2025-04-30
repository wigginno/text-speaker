using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextSpeaker.Models;
using TextSpeaker.Services;
using TextSpeaker.Views;

namespace TextSpeaker.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAzureSpeechService _speechService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<TopLevel?> _getTopLevelProvider;
    private List<VoiceDisplayItem> _allVoices = new(); // Cache all loaded voices for filtering

    // --- Properties ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private string _inputText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private VoiceDisplayItem? _selectedVoice;

    [ObservableProperty]
    private ObservableCollection<VoiceDisplayItem> _availableVoices = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableLanguages = new();

    [ObservableProperty]
    private string? _selectedLanguage;

    [ObservableProperty]
    private ObservableCollection<string> _availableRegions = new();

    [ObservableProperty]
    private string? _selectedRegion;

    [ObservableProperty]
    private ObservableCollection<string> _voiceTypeOptions = new() { "Neural", "Standard" };

    [ObservableProperty]
    private string _selectedVoiceType = "Neural";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Initializing..."; // Corrected initial text

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))] 
    private bool _areCredentialsConfigured;

    public MainWindowViewModel(IAzureSpeechService speechService, IDialogService dialogService, ISettingsService settingsService, IServiceProvider serviceProvider, Func<TopLevel?> getTopLevelProvider)
    {
        _speechService = speechService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _getTopLevelProvider = getTopLevelProvider;

        // Load voices asynchronously and log any potential errors
        _ = LoadVoicesAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
                Console.WriteLine($"FATAL EXCEPTION in LoadVoicesAsync task continuation: {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    // Helper to build a concise, user-friendly display name for a voice
    private static string FormatVoiceName(VoiceInfo voice)
    {
        if (voice == null) return "Unknown Voice";

        string baseName = voice.LocalName;
        try
        {
            var shortName = voice.ShortName;
            if (shortName.StartsWith(voice.Locale + "-"))
                shortName = shortName[(voice.Locale.Length + 1)..];
            foreach (var suf in new[] { "Neural", "Standard", "Latest" })
            {
                if (shortName.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                    shortName = shortName[..^suf.Length];
            }
            var separators = new[] { '-', ':' };
            baseName = shortName.Split(separators)[0];
        }
        catch { /* fallback to LocalName */ }

        var genderPart = voice.Gender != SynthesisVoiceGender.Unknown ? $" ({voice.Gender})" : string.Empty;
        return $"{baseName}{genderPart}";
    }

    [RelayCommand]
    private async Task LoadVoicesAsync()
    {
        if (!AreSettingsValid())
        {
            StatusText = $"Azure settings invalid or missing. Please configure via Settings.";
            return;
        }

        IsBusy = true;
        StatusText = "Loading voices..."; // Update status
        AvailableLanguages.Clear();
        AvailableRegions.Clear();
        AvailableVoices.Clear();
        _allVoices.Clear();
        SelectedLanguage = null;
        SelectedRegion = null;
        SelectedVoice = null;
        AreCredentialsConfigured = false; // Assume false initially

        try // Add top-level try-catch
        {
            var result = await _speechService.GetVoicesAsync();

            if (result.IsSuccess && result.Data != null) // Allow empty list on success
            {
                _allVoices = result.Data.Select(v => new VoiceDisplayItem(v, FormatVoiceName(v))).ToList();
                AreCredentialsConfigured = true; // Config OK if call succeeded
                StatusText = "Ready. Select voice and enter text."; // Set final status for SUCCESS case

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var languages = _allVoices
                        .Select(v => v.Voice.Locale.Split('-')[0])
                        .Distinct()
                        .Select(GetLanguageName)
                        .OrderBy(name => name)
                        .ToList();

                    AvailableLanguages.Clear();
                    foreach (var lang in languages) AvailableLanguages.Add(lang);

                    // Try to select English as default language if available
                    string englishName = GetLanguageName("en");
                    SelectedLanguage = AvailableLanguages.Contains(englishName) 
                        ? englishName 
                        : AvailableLanguages.FirstOrDefault();
                });
            }
            else
            {
                // Update status text and clear lists on the UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Set final status for FAILURE case on UI thread
                    StatusText = result.ErrorMessage ?? "Failed to load voices. Check Azure configuration.";
                    AreCredentialsConfigured = false; // Set flag on UI thread too
                    AvailableLanguages.Clear();
                    AvailableRegions.Clear();
                    AvailableVoices.Clear();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL EXCEPTION in LoadVoicesAsync: {ex.ToString()}"); // Log any exception
            // Optionally set status text here too, but ensure it's on UI thread if needed
            try { await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => StatusText = "Critical error during voice loading."); } catch { /* Ignore dispatcher errors during exception handling */ }
        }
        finally
        {
             IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveToFile))]
    private async Task SaveToFileAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || SelectedVoice == null)
        {
            StatusText = "Please enter text and select a voice.";
            return;
        }

        if (!AreSettingsValid()) // Redundant check for safety
        {
             StatusText = "Cannot synthesize: Azure settings invalid or missing.";
             await _dialogService.ShowMessageDialogAsync("Configuration Error", "Azure Speech Key and Region are not configured. Please use the Settings button.");
             return;
        }

        IsBusy = true;
        StatusText = "Saving audio file...";

        string filter = "MP3 files (*.mp3)|*.mp3";
        string defaultFileName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}.mp3";

        var outputPath = await _dialogService.ShowSaveFileDialogAsync("Save Audio As", "mp3", defaultFileName);

        if (!string.IsNullOrEmpty(outputPath))
        {
            var result = await _speechService.SynthesizeTextToFileAsync(InputText, SelectedVoice.Name, outputPath);
            if (result.IsSuccess)
            {
                StatusText = $"Audio saved successfully to {outputPath}";
            }
            else
            {
                StatusText = $"Failed to save audio: {result.ErrorMessage}";
            }
        }
        else
        {
            StatusText = "Save operation cancelled.";
        }

        IsBusy = false;
    }

    private bool CanSaveToFile() => !IsBusy && SelectedVoice != null && !string.IsNullOrWhiteSpace(InputText) && AreSettingsValid();

    [RelayCommand(CanExecute = nameof(CanSelectFile))]
    private async Task SelectFileAsync()
    {
        IsBusy = true;
        StatusText = "Loading text file...";

        string filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

        var filePath = await _dialogService.ShowOpenFileDialogAsync("Open Text File", filter);

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                InputText = await File.ReadAllTextAsync(filePath);
                StatusText = $"Text loaded from {filePath}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading file: {ex.Message}";
            }
        }
        else
        {
            StatusText = "Load operation cancelled.";
        }
        IsBusy = false;
    }
    private bool CanSelectFile() => !IsBusy;

    // Helper to get full language name
    private static string GetLanguageName(string code) =>
        LanguageCodeToName.TryGetValue(code, out var name) ? name : code;

    // Helper to get full region name
    private static string GetRegionName(string code) =>
        RegionCodeToName.TryGetValue(code, out var name) ? name : code;

    // --- Partial Methods for Property Changes ---
    partial void OnSelectedLanguageChanged(string? value)
    {
        _ = UpdateRegionsAndVoicesAsync();
    }

    partial void OnSelectedRegionChanged(string? value)
    {
        _ = UpdateVoicesForSelectionAsync();
    }

    partial void OnSelectedVoiceTypeChanged(string value)
    {
        _ = UpdateVoicesForSelectionAsync();
    }

    private async Task UpdateRegionsAndVoicesAsync()
    {
         if (string.IsNullOrEmpty(SelectedLanguage))
         {
             await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
             {
                 AvailableRegions.Clear();
                 AvailableVoices.Clear();
                 SelectedRegion = null;
                 SelectedVoice = null;
             });
             return;
         }

         string langCode = LanguageCodeToName.FirstOrDefault(x => x.Value == SelectedLanguage).Key ?? string.Empty;

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
             var regions = _allVoices
                .Where(v => v.Voice.Locale.StartsWith(langCode + "-"))
                .Select(v => v.Voice.Locale.Split('-')[1])
                .Distinct()
                .Select(GetRegionName)
                .OrderBy(name => name)
                .ToList();

            AvailableRegions.Clear();
            if (regions.Count > 1) 
            {
                 AvailableRegions.Add("All Regions"); 
                 foreach (var region in regions) AvailableRegions.Add(region);
            }

            if (!string.IsNullOrEmpty(SelectedRegion) && AvailableRegions.Contains(SelectedRegion))
            {
                // Keep existing selection
            }
            else if (AvailableRegions.Count > 1)
            {
                // For English language, try to default to United States if available
                if (SelectedLanguage == GetLanguageName("en"))
                {
                    string usName = GetRegionName("US");
                    if (AvailableRegions.Contains(usName))
                    {
                        SelectedRegion = usName;
                    }
                    else
                    {
                        SelectedRegion = "All Regions";
                    }
                }
                else
                {
                    SelectedRegion = "All Regions";
                }
            }
            else if (AvailableRegions.Count == 1)
            {
                SelectedRegion = AvailableRegions.First();
            }
            else
            {
                SelectedRegion = null;
            }
        });
    }

    private async Task UpdateVoicesForSelectionAsync()
    {
         if (string.IsNullOrEmpty(SelectedLanguage))
         {
             await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => AvailableVoices.Clear());
             return;
         }

         string langCode = LanguageCodeToName.FirstOrDefault(x => x.Value == SelectedLanguage).Key ?? string.Empty;
         string? regionCode = (SelectedRegion == "All Regions" || string.IsNullOrEmpty(SelectedRegion))
            ? null
            : RegionCodeToName.FirstOrDefault(x => x.Value == SelectedRegion).Key;

        var filteredByLang = _allVoices.Where(v => v.Voice.Locale.StartsWith(langCode));

        var filtered = filteredByLang
            .Where(v => regionCode == null || (v.Voice.Locale.Contains('-') && v.Voice.Locale.Split('-')[1] == regionCode))
            .Where(v => SelectedVoiceType == "Neural"
                ? (v.VoiceType == SynthesisVoiceType.OnlineNeural || v.VoiceType == SynthesisVoiceType.OfflineNeural)
                : (v.VoiceType == SynthesisVoiceType.OnlineStandard || v.VoiceType == SynthesisVoiceType.OfflineStandard))
            .OrderBy(v => v.DisplayName) 
            .ToList();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            AvailableVoices.Clear();
            foreach (var voice in filtered) AvailableVoices.Add(voice);

            if (SelectedVoice != null && AvailableVoices.Any(v => v.Name == SelectedVoice.Name))
            {
                SelectedVoice = AvailableVoices.First(v => v.Name == SelectedVoice.Name);
            }
            else if (AvailableVoices.Any())
            {
                 SelectedVoice = AvailableVoices.First();
            }
             else
             {
                 SelectedVoice = null;
             }
        });
         StatusText = $"Found {AvailableVoices.Count} {SelectedVoiceType} voices for {SelectedLanguage}{(regionCode == null ? "" : "/" + SelectedRegion)}.";
    }

    // Helper to check if settings are valid
    private bool AreSettingsValid()
    {
        var settings = _settingsService.CurrentSettings;
        return !string.IsNullOrWhiteSpace(settings?.AzureSpeechKey) &&
               !string.IsNullOrWhiteSpace(settings?.AzureSpeechRegion);
    }

    // --- Settings Command ---
    [RelayCommand]
    private async Task OpenSettingsAsync() // Changed to async Task
    {
        StatusText = "Opening settings...";
        try
        {
            // Resolve the SettingsViewModel and SettingsWindow
            var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            var settingsWindow = new SettingsWindow
            {
                DataContext = settingsViewModel
            };

            // Simply call ShowDialogAsync - our enhanced implementation in DialogService
            // will handle the case where no owner is found
            await _dialogService.ShowDialogAsync(settingsWindow, null);

            // After the settings window is closed:
            StatusText = "Refreshing configuration after settings change...";
            _speechService.RefreshConfiguration(); // Tell the service to re-read settings
            await LoadVoicesAsync(); // Reload voices which also updates status/CanExecute

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening or handling settings window: {ex}");
            StatusText = $"Error opening settings: {ex.Message}";
            // Consider showing a message dialog for the error
            await _dialogService.ShowMessageDialogAsync("Error", $"Failed to open settings: {ex.Message}");
        }
    }

    // Mappings (LanguageCodeToName, RegionCodeToName) - Keep these defined within the ViewModel or move to a separate static class if preferred
    private static readonly Dictionary<string, string> LanguageCodeToName = new()
    {
        { "en", "English" }, { "es", "Spanish" }, { "fr", "French" }, { "de", "German" }, { "it", "Italian" },
        { "pt", "Portuguese" }, { "ru", "Russian" }, { "zh", "Chinese" }, { "ja", "Japanese" }, { "ko", "Korean" },
        { "ar", "Arabic" }, { "hi", "Hindi" }, { "tr", "Turkish" }, { "nl", "Dutch" }, { "sv", "Swedish" },
        { "fi", "Finnish" }, { "no", "Norwegian" }, { "da", "Danish" }, { "pl", "Polish" }, { "cs", "Czech" },
        { "el", "Greek" }, { "he", "Hebrew" }, { "th", "Thai" }, { "id", "Indonesian" }, { "vi", "Vietnamese" },
        { "hu", "Hungarian" }, { "ro", "Romanian" }, { "sk", "Slovak" }, { "uk", "Ukrainian" }, { "bg", "Bulgarian" }
    };

    private static readonly Dictionary<string, string> RegionCodeToName = new()
    {
        { "US", "United States" }, { "GB", "United Kingdom" }, { "AU", "Australia" }, { "CA", "Canada" },
        { "IN", "India" }, { "ZA", "South Africa" }, { "NZ", "New Zealand" }, { "IE", "Ireland" },
        { "SG", "Singapore" }, { "PH", "Philippines" }, { "NG", "Nigeria" }, { "GH", "Ghana" },
        { "KE", "Kenya" }, { "HK", "Hong Kong" }, { "CN", "China" }, { "TW", "Taiwan" }, { "FR", "France" },
        { "DE", "Germany" }, { "IT", "Italy" }, { "ES", "Spain" }, { "BR", "Brazil" }, { "MX", "Mexico" },
        { "RU", "Russia" }, { "JP", "Japan" }, { "KR", "Korea" }, { "TR", "Turkey" }, { "NL", "Netherlands" },
        { "SE", "Sweden" }, { "FI", "Finland" }, { "NO", "Norway" }, { "DK", "Denmark" }, { "PL", "Poland" },
        { "CZ", "Czech Republic" }, { "GR", "Greece" }, { "IL", "Israel" }, { "TH", "Thailand" }, { "ID", "Indonesia" },
        { "VN", "Vietnam" }, { "HU", "Hungary" }, { "RO", "Romania" }, { "SK", "Slovakia" }, { "UA", "Ukraine" },
        { "BG", "Bulgaria" }
    };
}
