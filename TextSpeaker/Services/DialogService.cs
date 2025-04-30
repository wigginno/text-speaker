using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TextSpeaker.Services
{
    public class DialogService : IDialogService
    {
        private readonly Func<TopLevel?> _topLevelProvider;

        public DialogService(Func<TopLevel?> topLevelProvider)
        {
            _topLevelProvider = topLevelProvider;
        }

        private TopLevel GetTopLevel() => _topLevelProvider() ??
            throw new InvalidOperationException("Cannot perform dialog operation because the TopLevel window could not be determined.");

        public async Task<string?> ShowOpenFileDialogAsync(string title, string filter)
        {
            var topLevel = GetTopLevel();
            var storageProvider = topLevel.StorageProvider;

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = ParseFilter(filter)
            };

            var result = await storageProvider.OpenFilePickerAsync(options);
            var file = result.FirstOrDefault();
            return file?.TryGetLocalPath();
        }

        public async Task<string?> ShowSaveFileDialogAsync(string title, string defaultExtension, string? initialFileName = null)
        {
            var topLevel = GetTopLevel();
            var storageProvider = topLevel.StorageProvider;

            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = initialFileName,
                FileTypeChoices = ParseFilter($"{defaultExtension} files (*.{defaultExtension})|*.{defaultExtension}|All files (*.*)|*.*")
            };

            var result = await storageProvider.SaveFilePickerAsync(options);
            return result?.TryGetLocalPath();
        }

        public async Task ShowMessageDialogAsync(string title, string message)
        {
            // Basic message box implementation (Consider using a dedicated library for more features)
            var topLevel = GetTopLevel();
            if (topLevel is Window ownerWindow)
            {
                // This requires a specific message box implementation or library.
                // For simplicity, we'll just log it if no easy built-in exists.
                // Example using a hypothetical MessageBox.Show:
                // await MessageBox.Show(ownerWindow, message, title, MessageBoxButtons.Ok);
                Console.WriteLine($"Message Dialog ({title}): {message}"); // Placeholder
                // If you add a MessageBox library (like MessageBox.Avalonia), implement it here.
                await Task.CompletedTask; // Simulate async operation if needed
            }
            else
            {
                Console.WriteLine($"Message Dialog ({title}) - No Owner Window: {message}");
            }
        }

        // Implementation for showing a custom window as a modal dialog asynchronously
        public async Task ShowDialogAsync(Window window, Window? owner = null)
        {
            if (owner == null)
            {
                // If no owner is provided, try to get the top-level window
                owner = GetTopLevel() as Window;
            }

            if (owner != null)
            {
                // Only use ShowDialog if we have a valid owner
                await window.ShowDialog<object?>(owner); // Await the dialog closing
            }
            else
            {
                // Fallback to just showing the window non-modally if no owner
                window.Show();
                // Note: This doesn't wait for the window to close
                // Consider implementing a TaskCompletionSource approach if needed
            }
        }

        // Implementation for showing a custom dialog window
        public void ShowDialog(Window window)
        {
            var owner = GetTopLevel() as Window;
            if (owner != null)
            {
                window.ShowDialog(owner);
            }
            else
            {
                // Fallback if owner can't be determined (might show non-modally or centered)
                Console.WriteLine("Warning: Could not determine owner window for dialog. Showing window non-modally.");
                window.Show();
            }
        }

        private static System.Collections.Generic.List<FilePickerFileType> ParseFilter(string filter)
        {
            // Example filter: "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            var parts = filter.Split('|');
            var types = new System.Collections.Generic.List<FilePickerFileType>();
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var name = parts[i];
                var patterns = parts[i + 1].Split(';');
                types.Add(new FilePickerFileType(name) { Patterns = patterns });
            }
            return types;
        }
    }
}