using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using TextSpeaker.Services; // Placeholder for future services
using TextSpeaker.ViewModels; // Placeholder for future viewmodels

namespace TextSpeaker
{
    public class AzureSpeechConfiguration
    {
        public string? Key { get; set; }
        public string? Region { get; set; }
    }

    sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Uncomment if/when using JSON config
                .AddUserSecrets<Program>()
                .Build();

            var azureSpeechConfig = config.GetSection("AzureSpeech").Get<AzureSpeechConfiguration>();

            if (string.IsNullOrWhiteSpace(azureSpeechConfig?.Key) ||
                string.IsNullOrWhiteSpace(azureSpeechConfig?.Region))
            {
                throw new InvalidOperationException("AzureSpeech:Key and AzureSpeech:Region must be set in user secrets.");
            }

                        // Instantiate services here
                        var azureSpeechConfigObj = new AzureSpeechConfiguration
                        {
                            Key = azureSpeechConfig.Key!,
                            Region = azureSpeechConfig.Region!
                        };
                        var azureSpeechService = new AzureSpeechService(azureSpeechConfigObj);
                        Func<TopLevel?> topLevelProvider = () => App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                        var dialogService = new DialogService(topLevelProvider);

                        // Instantiate viewmodel here
                        var mainWindowViewModel = new MainWindowViewModel(azureSpeechService, dialogService);
            
                        try
                        {
                            BuildAvaloniaApp(mainWindowViewModel).StartWithClassicDesktopLifetime(args);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Application failed to start: {ex}");
                            throw;
                        }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp(MainWindowViewModel vm)
            => AppBuilder.Configure(() => new App(vm))
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
