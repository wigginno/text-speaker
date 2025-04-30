using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextSpeaker.Models;
using TextSpeaker.Services;

namespace TextSpeaker.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAzureSpeechService _speechService;
    private readonly IDialogService _dialogService;
    private List<VoiceDisplayItem> _allVoices = new();

    public MainWindowViewModel(IAzureSpeechService speechService, IDialogService dialogService)
    {
        _speechService = speechService;
        _dialogService = dialogService;
        _ = LoadVoicesAsync();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private string _inputText = "";

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

    // Mappings for language and region codes to full names
    private static readonly Dictionary<string, string> LanguageCodeToName = new()
    {
        { "en", "English" }, { "es", "Spanish" }, { "fr", "French" }, { "de", "German" }, { "it", "Italian" },
        { "pt", "Portuguese" }, { "ru", "Russian" }, { "zh", "Chinese" }, { "ja", "Japanese" }, { "ko", "Korean" },
        { "ar", "Arabic" }, { "hi", "Hindi" }, { "tr", "Turkish" }, { "nl", "Dutch" }, { "sv", "Swedish" },
        { "fi", "Finnish" }, { "no", "Norwegian" }, { "da", "Danish" }, { "pl", "Polish" }, { "cs", "Czech" },
        { "el", "Greek" }, { "he", "Hebrew" }, { "th", "Thai" }, { "id", "Indonesian" }, { "vi", "Vietnamese" },
        { "hu", "Hungarian" }, { "ro", "Romanian" }, { "sk", "Slovak" }, { "uk", "Ukrainian" }, { "bg", "Bulgarian" }
        // Add more as needed
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
        // Add more as needed
    };

    // Helper to get full language name
    private static string GetLanguageName(string code) =>
        LanguageCodeToName.TryGetValue(code, out var name) ? name : code;

    // Helper to get full region name
    private static string GetRegionName(string code) =>
        RegionCodeToName.TryGetValue(code, out var name) ? name : code;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private VoiceDisplayItem? _selectedVoice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "ready.";

    // Helper to build a concise, user-friendly display name for a voice
    private static string FormatVoiceName(VoiceInfo voice)
    {
        if (voice == null) return "Unknown Voice";

        // Derive a base voice name from ShortName or LocalName
        string baseName = voice.LocalName;
        try
        {
            var shortName = voice.ShortName;
            // Remove locale prefix
            if (shortName.StartsWith(voice.Locale + "-"))
                shortName = shortName[(voice.Locale.Length + 1)..];
            // Remove common suffixes
            foreach (var suf in new[] { "Neural", "Standard", "Latest" })
            {
                if (shortName.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                    shortName = shortName[..^suf.Length];
            }
            // If still contains dash or colon, take first segment
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
        IsBusy = true;
        StatusText = "Loading voices...";
        AvailableVoices.Clear();
        try
        {
            // Map selected language & region names back to locale code
            string langCode = LanguageCodeToName.FirstOrDefault(x => x.Value == SelectedLanguage).Key ?? "en";
            string regionCode = RegionCodeToName.FirstOrDefault(x => x.Value == SelectedRegion).Key ?? "US";
            string locale = $"{langCode}-{regionCode}";
            var voices = await _speechService.GetVoicesAsync(locale);
            _allVoices = voices.Select(v => new VoiceDisplayItem(v, FormatVoiceName(v))).ToList();
            // Ensure UI-bound collection is updated on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Extract unique languages and regions
                var languages = _allVoices
                    .Select(v => v.Voice.Locale.Split('-')[0])
                    .Distinct()
                    .OrderBy(l => GetLanguageName(l))
                    .ToList();
                AvailableLanguages.Clear();
                foreach (var lang in languages)
                    AvailableLanguages.Add(GetLanguageName(lang));

                // Set default language to English if available, otherwise first
                SelectedLanguage = AvailableLanguages.Contains("English") ? "English" : AvailableLanguages.FirstOrDefault();

                // Extract unique regions for the selected language
                UpdateRegionsAndVoices(_allVoices);
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load voices: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedLanguageChanged(string? value)
    {
        // When language changes, update regions and voices
        _ = UpdateRegionsAndVoicesAsync();
    }

    partial void OnSelectedRegionChanged(string? value)
    {
        // When region changes, update voices
        _ = UpdateVoicesForSelectionAsync();
    }

    partial void OnSelectedVoiceTypeChanged(string value)
    {
        // Refresh voice list when user toggles neural/standard
        _ = UpdateVoicesForSelectionAsync();
    }

    private async Task UpdateRegionsAndVoicesAsync()
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateRegionsAndVoices(_allVoices);
        });
    }

    private void UpdateRegionsAndVoices(List<VoiceDisplayItem> voices)
    {
        // Find the language code for the selected language name
        string? langCode = LanguageCodeToName.FirstOrDefault(x => x.Value == SelectedLanguage).Key;
        // Filter voices by selected language code
        var filteredByLang = voices
            .Where(v => langCode == null || v.Voice.Locale.StartsWith(langCode))
            .ToList();

        // Extract unique regions
        var regions = filteredByLang
            .Select(v => v.Voice.Locale.Contains('-') ? v.Voice.Locale.Split('-')[1] : v.Voice.Locale)
            .Distinct()
            .OrderBy(r => GetRegionName(r))
            .ToList();

        AvailableRegions.Clear();
        foreach (var region in regions)
            AvailableRegions.Add(GetRegionName(region));

        // Set default region to United States if available, otherwise first
        SelectedRegion = AvailableRegions.Contains("United States") ? "United States" : AvailableRegions.FirstOrDefault();

        // Now update voices for this selection
        UpdateVoicesForSelection(filteredByLang);
    }

    private async Task UpdateVoicesForSelectionAsync()
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            string? langCode = LanguageCodeToName.FirstOrDefault(x => x.Value == SelectedLanguage).Key;
            var filteredByLang = _allVoices
                .Where(v => langCode == null || v.Voice.Locale.StartsWith(langCode))
                .ToList();
            UpdateVoicesForSelection(filteredByLang);
        });
    }

    private void UpdateVoicesForSelection(List<VoiceDisplayItem> filteredByLang)
    {
        // Further filter by region
        // Find the region code for the selected region name
        string? regionCode = RegionCodeToName.FirstOrDefault(x => x.Value == SelectedRegion).Key;
        var filtered = filteredByLang
            .Where(v => regionCode == null || (v.Voice.Locale.Contains('-') && v.Voice.Locale.Split('-')[1] == regionCode))
            .Where(v => SelectedVoiceType == "Neural"
                ? (v.VoiceType == SynthesisVoiceType.OnlineNeural || v.VoiceType == SynthesisVoiceType.OfflineNeural)
                : (v.VoiceType == SynthesisVoiceType.OnlineStandard || v.VoiceType == SynthesisVoiceType.OfflineStandard))
            .ToList();

        AvailableVoices.Clear();
        foreach (var voice in filtered.OrderBy(v => v.DisplayName))
            AvailableVoices.Add(voice);

        SelectedVoice = AvailableVoices.FirstOrDefault();
        StatusText = filtered.Count > 0 ? $"Loaded {filtered.Count} voices." : "No voices found.";
    }

    [RelayCommand(CanExecute = nameof(CanSelectFile))]
    private async Task SelectFileAsync()
    {
        StatusText = "Selecting file...";
        var filePath = await _dialogService.ShowOpenFileDialogAsync("Select a text file", "Text files (*.txt)|*.txt|All files (*.*)|*.*");
        if (!string.IsNullOrEmpty(filePath))
        {
            IsBusy = true;
            try
            {
                InputText = await File.ReadAllTextAsync(filePath);
                StatusText = $"Loaded file: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to load file: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        else
        {
            StatusText = "File selection cancelled.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task SaveToFileAsync()
    {
        StatusText = "Selecting output file...";
        var filePath = await _dialogService.ShowSaveFileDialogAsync("Save MP3 file", "MP3 files (*.mp3)|*.mp3", $"output_{DateTime.Now:yyyyMMdd_HHmmss}.mp3");
        if (!string.IsNullOrEmpty(filePath))
        {
            IsBusy = true;
            try
            {
                var result = await _speechService.SynthesizeTextToFileAsync(InputText, SelectedVoice!.Voice.Name, filePath);
                if (result)
                {
                    StatusText = $"Audio saved to: {Path.GetFileName(filePath)}";
                }
                else
                {
                    StatusText = "Failed to synthesize audio.";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        else
        {
            StatusText = "Save operation cancelled.";
        }
    }

    private bool CanSelectFile() => !IsBusy;

    private bool CanProcess() => !IsBusy && !string.IsNullOrWhiteSpace(InputText) && SelectedVoice != null;
}
