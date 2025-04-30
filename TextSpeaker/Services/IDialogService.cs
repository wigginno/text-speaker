using Avalonia.Controls; // Required for Window
using System.Threading.Tasks;

namespace TextSpeaker.Services
{
    public interface IDialogService
    {
        // Ensure this matches the design-time service
        Task<string?> ShowOpenFileDialogAsync(string title, string filter);

        // Correct signature matching DialogService implementation
        Task<string?> ShowSaveFileDialogAsync(string title, string defaultExtension, string? initialFileName = null);

        // Re-added missing signature
        Task ShowMessageDialogAsync(string title, string message);

        // Changed method to show a generic Window as a modal dialog asynchronously
        Task ShowDialogAsync(Window window, Window? owner = null);
    }
}