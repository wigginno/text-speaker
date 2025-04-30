using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextSpeaker.Models; // Required for ServiceResult and Settings

namespace TextSpeaker.Services
{
    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly ISettingsService _settingsService; // Changed from IConfiguration
        private SpeechConfig? _speechConfig;
        private bool _isConfigValid = false;

        // Constructor updated to inject ISettingsService
        public AzureSpeechService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            UpdateSpeechConfig();
            // Optional: Hook into a settings changed event if ISettingsService provides one
            // to automatically update the config when settings are saved externally.
        }

        // Helper method to create/update SpeechConfig based on current settings
        private void UpdateSpeechConfig()
        {
            var settings = _settingsService.CurrentSettings;
            string? key = settings?.AzureSpeechKey;
            string? region = settings?.AzureSpeechRegion;

            _isConfigValid = !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(region);

            if (_isConfigValid)
            {
                _speechConfig = SpeechConfig.FromSubscription(key!, region!);
                // Optional: Configure logging for the SDK
                // _speechConfig.SetProperty(PropertyId.Speech_LogFilename, "speech_sdk_log.txt");
            }
            else
            {
                _speechConfig = null; // Ensure config is null if settings are invalid
            }
        }

        // Method to explicitly refresh config if settings might have changed
        public void RefreshConfiguration()
        {
            UpdateSpeechConfig();
        }

        // Removed locale parameter
        public async Task<ServiceResult<List<VoiceInfo>>> GetVoicesAsync()
        {
            // Ensure config is up-to-date before use
            // (Could be redundant if we assume config is always fresh, but safe)
            RefreshConfiguration();

            if (!_isConfigValid || _speechConfig == null)
            {
                return new ServiceResult<List<VoiceInfo>>(false, null, "Azure credentials not configured. Please check Settings.");
            }

            var voices = new List<VoiceInfo>();
            try
            {
                using var synthesizer = new SpeechSynthesizer(_speechConfig);
                using var result = await synthesizer.GetVoicesAsync();

                if (result.Reason == ResultReason.VoicesListRetrieved)
                {
                    voices.AddRange(result.Voices);
                    return new ServiceResult<List<VoiceInfo>>(true, voices.OrderBy(v => v.LocalName).ToList(), null);
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    // Simplified cancellation handling for GetVoicesAsync
                    // CancellationDetails.FromResult is not applicable to SynthesisVoicesResult
                    string errorDetails = $"Voice retrieval was canceled. ResultId: {result.ResultId}";
                    Console.WriteLine(errorDetails); // Log the cancellation
                    return new ServiceResult<List<VoiceInfo>>(false, null, errorDetails);
                }
                else // Should not happen based on SDK docs for GetVoicesAsync
                {
                    return new ServiceResult<List<VoiceInfo>>(false, null, $"Unknown error retrieving voices. ResultReason: {result.Reason}");
                }
            }
            catch (Exception ex)
            {
                // Catch potential exceptions during SDK interaction (e.g., network issues)
                Console.WriteLine($"Exception in GetVoicesAsync: {ex}");
                return new ServiceResult<List<VoiceInfo>>(false, null, $"An unexpected error occurred: {ex.Message}");
            }
        }

        // Return type explicitly set to ServiceResult<bool>
        public async Task<ServiceResult<bool>> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath)
        {
            // Ensure config is up-to-date
            RefreshConfiguration();

            if (!_isConfigValid || _speechConfig == null)
            {
                return new ServiceResult<bool>(false, false, "Azure credentials not configured. Please check Settings.");
            }
            if (string.IsNullOrEmpty(text))
            {
                return new ServiceResult<bool>(false, false, "Input text cannot be empty.");
            }
            if (string.IsNullOrEmpty(voiceName))
            {
                 return new ServiceResult<bool>(false, false, "Voice name must be selected.");
            }
            if (string.IsNullOrEmpty(outputFilePath))
            {
                 return new ServiceResult<bool>(false, false, "Output file path cannot be empty.");
            }

            try
            {
                // Set the desired voice on the config *before* creating the synthesizer
                _speechConfig.SpeechSynthesisVoiceName = voiceName;

                // Set the output format to MP3
                _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

                // For MP3 format, we need to use a FileStream to save the result after synthesis
                // First create a synthesizer without output file
                using var synthesizer = new SpeechSynthesizer(_speechConfig);

                // Synthesize the text to audio stream (MP3 format as configured by SetSpeechSynthesisOutputFormat)
                using var result = await synthesizer.SpeakTextAsync(text);

                // Check the result
                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    // Write the audio data to the specified file
                    using (var fileStream = File.Create(outputFilePath))
                    {
                        var audioData = result.AudioData;
                        await fileStream.WriteAsync(audioData, 0, audioData.Length);
                    }
                    
                    Console.WriteLine($"Speech synthesized for text [{text.Substring(0, Math.Min(text.Length, 20))}...] to [{outputFilePath}]");
                    return new ServiceResult<bool>(true, true, null);
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"Speech synthesis canceled: Reason={cancellation.Reason}");
                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"Speech synthesis canceled: ErrorCode={cancellation.ErrorCode}, ErrorDetails=[{cancellation.ErrorDetails}]\nDid you update the subscription info?");
                        return new ServiceResult<bool>(false, false, $"Synthesis failed: {cancellation.ErrorDetails}");
                    }
                    return new ServiceResult<bool>(false, false, $"Synthesis canceled: {cancellation.Reason}");
                }
                else
                {
                    Console.WriteLine($"Speech synthesis completed with unexpected reason: {result.Reason}");
                    return new ServiceResult<bool>(false, false, $"Synthesis failed with unexpected reason: {result.Reason}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception in SynthesizeTextToFileAsync: {ex}");
                return new ServiceResult<bool>(false, false, $"An unexpected error occurred during synthesis: {ex.Message}");
            }
        }
    }
}