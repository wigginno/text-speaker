using System.Threading.Tasks;
using TextSpeaker.Models;

namespace TextSpeaker.Services
{
    public interface ISettingsService
    {
        Settings CurrentSettings { get; }
        Task LoadSettingsAsync();
        Task SaveSettingsAsync(Settings settings);
        string GetSettingsFilePath(); // Helper to know where settings are stored
    }
}
