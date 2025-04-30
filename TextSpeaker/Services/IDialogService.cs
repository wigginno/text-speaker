using Avalonia.Controls;
using System.Threading.Tasks;

namespace TextSpeaker.Services
{
    public interface IDialogService
    {
        Task<string?> ShowOpenFileDialogAsync(string title, string filter);
        Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName);
    }
}