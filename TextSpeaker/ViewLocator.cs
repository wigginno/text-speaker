using Avalonia.Controls;
using Avalonia.Controls.Templates;
using TextSpeaker.ViewModels;
using TextSpeaker.Views; // Add this using directive for View types

namespace TextSpeaker;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Replace reflection with explicit checks for known ViewModel types
        if (param is MainWindowViewModel)
        {
            return new MainWindow();
        }
        else if (param is SettingsViewModel) // Assuming SettingsViewModel exists and maps to SettingsWindow
        {
             return new SettingsWindow();
        }
        // Fallback if no matching view is found
        var name = param.GetType().FullName; // Get the original ViewModel name for the error message
        return new TextBlock { Text = "View Not Found for ViewModel: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
