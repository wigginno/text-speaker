using Avalonia;
using Avalonia.Controls; 
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using TextSpeaker.Services;
using TextSpeaker.ViewModels;
using TextSpeaker.Views;

namespace TextSpeaker
{
    sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) 
        {
            // --- Configuration Setup ---
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "TextSpeaker");
            Directory.CreateDirectory(settingsDir); 
            var settingsPath = Path.Combine(settingsDir, "settings.json");

            // Build configuration incorporating settings.json, env vars, and command line
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(settingsPath, optional: true, reloadOnChange: true) 
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // --- Dependency Injection Setup ---
            var services = new ServiceCollection();
            ConfigureServices(services, configuration, settingsPath); 

            // --- Provide DI Container to App ---
            var serviceProvider = services.BuildServiceProvider();
            App.Services = serviceProvider; 

            // --- Build and Run Avalonia App ---
            try
            {
                var appBuilder = BuildAvaloniaApp();
                appBuilder.StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
            }
            catch (Exception e)
            {
                Console.WriteLine($"FATAL: Avalonia startup failed: {e}");
                // Consider logging to a file or showing a message box if possible before exiting
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
                // .UseReactiveUI(); 

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, string settingsPath)
        {
            // Register IConfiguration (built from multiple sources)
            services.AddSingleton(configuration);

            // Register Settings Service (Singleton) - Pass the specific path
            services.AddSingleton<ISettingsService>(sp => new SettingsService(settingsPath));

            // Register other services
            // AzureSpeechService now depends on IConfiguration
            services.AddSingleton<IAzureSpeechService, AzureSpeechService>();

            // Dialog Service needs TopLevel provider
            services.AddSingleton<Func<TopLevel?>>(GetTopLevelProvider); 
            services.AddSingleton<IDialogService, DialogService>();

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SettingsViewModel>();
        }

        // Helper to get TopLevel provider function
        private static Func<TopLevel?> GetTopLevelProvider(IServiceProvider sp)
        {
            // This lambda captures the IServiceProvider 'sp' if needed, but currently doesn't use it.
            // It provides a function that can be called later to get the TopLevel.
            return () =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    // Return the MainWindow or the active TopLevel associated with it.
                    // Using lifetime.MainWindow is generally sufficient.
                    return lifetime.MainWindow;
                }
                // Log or handle the case where the lifetime or MainWindow isn't available yet or anymore.
                Console.WriteLine("Warning: Could not get TopLevel window. Application lifetime or MainWindow is null.");
                return null;
            };
        }
    }
}
