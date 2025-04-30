using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextSpeaker.Services
{
    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly AzureSpeechConfiguration _config;
        private const int MaxChunkSize = 4000;

        public AzureSpeechService(AzureSpeechConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Key))
                throw new ArgumentException("Azure Speech key is required.", nameof(config.Key));
            if (string.IsNullOrWhiteSpace(config.Region))
                throw new ArgumentException("Azure Speech region is required.", nameof(config.Region));

            _config = config;
        }

        public async Task<List<VoiceInfo>> GetVoicesAsync(string? locale = null)
        {
            try
            {
                var speechConfig = SpeechConfig.FromSubscription(_config.Key, _config.Region);
                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                var result = locale == null
                    ? await synthesizer.GetVoicesAsync()
                    : await synthesizer.GetVoicesAsync(locale);
                if (result.Reason == ResultReason.VoicesListRetrieved)
                {
                    return result.Voices.ToList();
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve voices: {result.Reason}");
                    return new List<VoiceInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetVoicesAsync: {ex}");
                return new List<VoiceInfo>();
            }
        }

        public async Task<bool> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath)
        {
            var speechConfig = SpeechConfig.FromSubscription(_config.Key, _config.Region);
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            using var audioConfig = AudioConfig.FromWavFileOutput(outputFilePath);
            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

            bool overallSuccess = true;
            var chunks = SplitTextIntoChunks(text, MaxChunkSize);

            foreach (var chunk in chunks)
            {
                var result = await synthesizer.SpeakTextAsync(chunk);
                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    // Success
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"Synthesis canceled: {cancellation.Reason}, {cancellation.ErrorDetails}");
                    overallSuccess = false;
                }
                else
                {
                    Console.WriteLine($"Synthesis failed: {result.Reason}");
                    overallSuccess = false;
                }
            }

            return overallSuccess;
        }

        private static List<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            int current = 0;
            while (current < text.Length)
            {
                int length = Math.Min(maxChunkSize, text.Length - current);
                chunks.Add(text.Substring(current, length));
                current += length;
            }
            return chunks;
        }
    }
}