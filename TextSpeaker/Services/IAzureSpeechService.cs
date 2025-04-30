using Microsoft.CognitiveServices.Speech;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TextSpeaker.Services
{
    public interface IAzureSpeechService
    {
        Task<List<VoiceInfo>> GetVoicesAsync(string? locale = null);
        Task<bool> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath);
    }
}