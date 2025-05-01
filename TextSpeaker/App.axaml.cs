using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TextSpeaker.ViewModels;
using TextSpeaker.Views;
using TextSpeaker.Services;
using TextSpeaker.Models;
using Microsoft.CognitiveServices.Speech;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TextSpeaker;

public partial class App : Application
{
    // Make setter public to allow assignment from Program.cs
    public static IServiceProvider? Services { get; set; }

    public App() { }

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
            // DisableAvaloniaDataAnnotationValidation(); // Temporarily comment out

            // Temporarily comment out the line causing the build error
            // Avalonia.Data.Core.ExpressionObserver.DataValidators.RemoveAll(x => x is Avalonia.Data.Core.Plugins.DataAnnotationsValidationPlugin);

            // Check if DI provider is available and handle null case
            if (Services == null)
            {
                // Log or handle the error appropriately - this indicates a failure in Program.cs setup
                Console.WriteLine("FATAL: ServiceProvider not initialized in App. Cannot create MainWindow.");
                // Optionally, throw an exception or exit
                desktop.Shutdown(-1); // Exit with an error code
                return;
            }

            // Resolve MainWindow and its ViewModel using the service provider
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Comment out the related method as well
    /*
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<Avalonia.Data.Core.Plugins.DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    */

    // --- Design Time Services (Mock Implementations) ---
    // These are used by the Avalonia XAML previewer

    // Updated DesignTimeAzureSpeechService
    private class DesignTimeAzureSpeechService : IAzureSpeechService
    {
        // Updated signature: removed locale, returns Task<ServiceResult<List<VoiceInfo>>>
        public Task<ServiceResult<List<VoiceInfo>>> GetVoicesAsync()
        {
            // Return sample data for the designer
            var sampleVoices = new List<VoiceInfo>
            {
                // Using constructor directly if VoiceInfo is a class/record we control
                // Or create mock VoiceInfo objects if needed
                // Placeholder: Assuming VoiceInfo has a constructor or properties can be set
                // new VoiceInfo { DisplayName = "English (United States) - Jenny", ShortName = "en-US-JennyNeural", Locale = "en-US" },
                // new VoiceInfo { DisplayName = "Spanish (Mexico) - Jorge", ShortName = "es-MX-JorgeNeural", Locale = "es-MX" }
                // Since VoiceInfo is from SDK, we might not be able to instantiate directly.
                // Return an empty list or handle appropriately for design time.
            };
            // Added null for ErrorMessage
            return Task.FromResult(new ServiceResult<List<VoiceInfo>>(true, new List<VoiceInfo>(), null)); // Return empty list for simplicity
        }

        // Updated signature: returns Task<ServiceResult<bool>>
        public Task<ServiceResult<bool>> SynthesizeTextToFileAsync(string text, string voiceName, string outputFilePath, IProgress<string> progress)
        {
            // Simulate success for design time
            progress?.Report("Design-time synthesis complete.");
            return Task.FromResult(new ServiceResult<bool>(true, true, null));
        }

        // Added implementation for RefreshConfiguration
        public void RefreshConfiguration()
        {
            // No-op for design time
            Console.WriteLine("[Design Time] RefreshConfiguration called.");
        }
    }

    // Updated DesignTimeDialogService
    private class DesignTimeDialogService : IDialogService
    {
        // Added missing implementation for ShowOpenFileDialogAsync
        public Task<string?> ShowOpenFileDialogAsync(string title, string filter)
        {
            Console.WriteLine($"[Design Time] ShowOpenFileDialogAsync: Title='{title}', Filter='{filter}'");
            return Task.FromResult<string?>("/design/time/dummy/open.txt"); // Return a dummy path
        }

        // Corrected signature based on actual IDialogService (using defaultExtension, initialFileName)
        public Task<string?> ShowSaveFileDialogAsync(string title, string defaultExtension, string? initialFileName = null)
        {
            Console.WriteLine($"[Design Time] ShowSaveFileDialogAsync: Title='{title}', DefaultExt='{defaultExtension}', InitialFileName='{initialFileName}'");
            return Task.FromResult<string?>("/design/time/path/output.mp3"); // Return a dummy path
        }

        public Task ShowMessageDialogAsync(string title, string message)
        {
            Console.WriteLine($"[Design Time] ShowMessageDialogAsync: {title} - {message}");
            return Task.CompletedTask;
        }

        // Added implementation for ShowDialogAsync (was ShowDialog)
        public Task ShowDialogAsync(Window window, Window? owner = null) // Updated signature to match interface
        {
            // No-op for design time, maybe log
            Console.WriteLine($"[Design Time] ShowDialogAsync called for window: {window?.Title ?? "(null)"} owned by {owner?.Title ?? "(null)"}");
            // Do not actually show the window here as it can interfere with the designer
            return Task.CompletedTask; // Return completed task for async method
        }
    }
}