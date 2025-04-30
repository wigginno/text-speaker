using Avalonia.Controls;
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

        public async Task<string?> ShowOpenFileDialogAsync(string title, string filter)
        {
            var topLevel = GetTopLevel();
            if (topLevel == null)
                return null;

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = ParseFilter(filter)
            };

            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            var file = result.FirstOrDefault();
            return file?.TryGetLocalPath();
        }

        public async Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName)
        {
            var topLevel = GetTopLevel();
            if (topLevel == null)
                return null;

            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFileName,
                FileTypeChoices = ParseFilter(filter)
            };

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            return result?.TryGetLocalPath();
        }

        private TopLevel? GetTopLevel()
        {
            return _topLevelProvider();
        }

        private static FilePickerFileType[] ParseFilter(string filter)
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
            return types.ToArray();
        }
    }
}