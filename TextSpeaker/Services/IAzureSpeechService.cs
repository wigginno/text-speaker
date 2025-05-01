using System;
using Microsoft.CognitiveServices.Speech;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextSpeaker.Models; 

namespace TextSpeaker.Services
{
    public interface IAzureSpeechService
    {
        Task<ServiceResult<List<VoiceInfo>>> GetVoicesAsync();
        Task<ServiceResult<bool>> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath, IProgress<string> progress);
        void RefreshConfiguration();
    }
}