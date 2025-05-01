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

        private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text)) return chunks;

            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int length = Math.Min(maxChunkSize, text.Length - startIndex);

                if (startIndex + length < text.Length)
                {
                    int potentialEnd = startIndex + length;
                    // Look for sentence-ending punctuation or newline within the last half of the chunk for a better split point
                    int searchStart = Math.Max(startIndex, potentialEnd - (length / 2));
                    int searchLength = potentialEnd - searchStart;
                    // Ensure searchLength is not negative if potentialEnd - (length / 2) is less than startIndex
                    if (searchLength < 0) searchLength = 0;
                    // Ensure search index is within bounds
                    int searchIndex = potentialEnd - 1;
                    if (searchIndex >= text.Length) searchIndex = text.Length - 1;
                    if (searchIndex < 0) searchIndex = 0; // Should not happen, but safety check
                    // Adjust search length if it goes beyond text length
                    if (searchIndex + searchLength > text.Length) searchLength = text.Length - searchIndex;
                    
                    int lastPunctuation = -1;
                    if (searchLength > 0 && searchIndex >= 0) // Only search if valid range
                    {
                       lastPunctuation = text.LastIndexOfAny(new[] { '.', '!', '?', '\n' }, searchIndex , searchLength);
                    }


                    if (lastPunctuation > startIndex)
                    {
                        length = lastPunctuation - startIndex + 1;
                    }
                    // If no natural break found, stick with maxChunkSize
                }

                chunks.Add(text.Substring(startIndex, length).Trim());
                startIndex += length;
            }
            return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
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
        public async Task<ServiceResult<bool>> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath, IProgress<string> progress)
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

            // --- Start Chunking Modification ---
            const int maxChunkSize = 1800; // Characters per chunk (adjust as needed, consider Azure limits/testing)
            var textChunks = SplitTextIntoChunks(text, maxChunkSize);

            if (!textChunks.Any())
            {
                return new ServiceResult<bool>(false, false, "Input text resulted in no processable chunks.");
            }

            Console.WriteLine($"Input text split into {textChunks.Count} chunks for synthesis.");

            try
            {
                // Set the desired voice on the config *before* creating the synthesizer
                _speechConfig.SpeechSynthesisVoiceName = voiceName;

                // Set the output format to MP3
                _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

                // Create the synthesizer ONCE - it can be reused for multiple chunks
                using var synthesizer = new SpeechSynthesizer(_speechConfig);

                // Open the output file stream ONCE to append audio data from all chunks
                using (var fileStream = File.Create(outputFilePath))
                {
                    for (int i = 0; i < textChunks.Count; i++)
                    {
                        string chunk = textChunks[i];
                        progress?.Report($"Synthesizing chunk {i + 1}/{textChunks.Count}...");

                        // Synthesize the current chunk
                        using var result = await synthesizer.SpeakTextAsync(chunk);

                        // Check the result for the current chunk
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            var audioData = result.AudioData;
                            if (audioData != null && audioData.Length > 0)
                            {
                                // Append the audio data of the current chunk to the file
                                await fileStream.WriteAsync(audioData, 0, audioData.Length);
                                Console.WriteLine($"Appended audio for chunk {i + 1}.");
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Synthesizing chunk {i + 1} resulted in empty audio data (Text: '{chunk.Substring(0, Math.Min(chunk.Length, 50))}...'). Skipping append.");
                                // Decide if this is an error or just skippable (e.g., chunk was only whitespace after trim)
                            }
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                            string errorMsg = $"Synthesis canceled on chunk {i + 1}: Reason={cancellation.Reason}";
                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                errorMsg += $", ErrorCode={cancellation.ErrorCode}, ErrorDetails=[{cancellation.ErrorDetails}]";
                                Console.WriteLine($"{errorMsg}\nDid you update the subscription info?");
                            }
                            else
                            {
                                 Console.WriteLine(errorMsg);
                            }
                            // Clean up the partially created file as the synthesis failed
                            try { fileStream.Close(); File.Delete(outputFilePath); } catch { /* Ignore errors during cleanup */ }
                            return new ServiceResult<bool>(false, false, $"Synthesis failed on chunk {i + 1}: {cancellation.Reason}{(cancellation.Reason == CancellationReason.Error ? $" ({cancellation.ErrorDetails})" : "")}");
                        }
                        else // Other unexpected reasons
                        {
                            string errorMsg = $"Speech synthesis for chunk {i + 1} completed with unexpected reason: {result.Reason}";
                            Console.WriteLine(errorMsg);
                            // Clean up the partially created file
                            try { fileStream.Close(); File.Delete(outputFilePath); } catch { /* Ignore errors during cleanup */ }
                            return new ServiceResult<bool>(false, false, $"Synthesis failed on chunk {i + 1} with unexpected reason: {result.Reason}");
                        }
                    } // End loop through chunks
                } // FileStream is automatically closed and disposed here, saving the complete file.

                progress?.Report($"Speech synthesized successfully for all {textChunks.Count} chunks to [{outputFilePath}]");
                return new ServiceResult<bool>(true, true, null); // Overall success
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception during chunked synthesis in SynthesizeTextToFileAsync: {ex}");
                // Attempt to clean up potentially partially written file
                try { File.Delete(outputFilePath); } catch { /* Ignore delete error */ }
                return new ServiceResult<bool>(false, false, $"An unexpected error occurred during synthesis: {ex.Message}");
            }
            // --- End Chunking Modification ---
        }
    }
}