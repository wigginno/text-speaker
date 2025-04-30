using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using TextSpeaker.ViewModels;
using TextSpeaker.Views;
using TextSpeaker.Services;
using Microsoft.CognitiveServices.Speech;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

using System.Collections.Generic; // For List<T>

namespace TextSpeaker;
 
public partial class App : Application
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public App(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    // Parameterless constructor for XAML previewer
    public App() : this(null!) { }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            MainWindowViewModel viewModel;
            if (_mainWindowViewModel != null)
            {
                viewModel = _mainWindowViewModel;
            }
            else
            {
                // Design-time/dummy services for XAML previewer
                var speechService = new DesignTimeAzureSpeechService();
                var dialogService = new DesignTimeDialogService();
                viewModel = new MainWindowViewModel(speechService, dialogService);
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
 
// Dummy services for design-time/XAML previewer
public class DesignTimeAzureSpeechService : IAzureSpeechService
{
    // Corrected to match interface: no parameters
    public Task<List<VoiceInfo>> GetVoicesAsync(string? locale = null)
    {
        // Return an empty list for design time (VoiceInfo constructor is internal)
        return Task.FromResult(new List<VoiceInfo>());
    }

    public Task<bool> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath)
    {
        Console.WriteLine($"[Design Time] Synthesize: Text='{(text?.Length > 20 ? text.Substring(0, 20) : text)}', Voice='{voiceName}', Path='{outputFilePath}'");
        return Task.FromResult(true);
    }
}

public class DesignTimeDialogService : IDialogService
{
    // Corrected to match interface: (string title, string filter)
    public Task<string?> ShowOpenFileDialogAsync(string title, string filter)
    {
        Console.WriteLine($"[Design Time] ShowOpenFileDialogAsync: Title='{title}', Filter='{filter}'");
        return Task.FromResult<string?>("/design/time/dummy/open.txt");
    }

    // Corrected to match interface: (string title, string filter, string defaultFileName)
    public Task<string?> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName)
    {
        Console.WriteLine($"[Design Time] ShowSaveFileDialogAsync: Title='{title}', Filter='{filter}', Default='{defaultFileName}'");
        return Task.FromResult<string?>("/design/time/dummy/save.mp3");
    }
}