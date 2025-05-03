using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp;
using BorderlessWindowApp.Services.Display.implement;
using BorderlessWindowApp.Services.Presets;
using BorderlessWindowApp.ViewModels;
using BorderlessWindowApp.ViewModels.Display;

namespace BorderlessWindowApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!; // Expose ServiceProvider

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // --- Configure Logging ---
            services.AddLogging(configure =>
            {
                configure.AddDebug(); // Output logs to the Debug window
                // configure.AddConsole(); // Optional: Output to console
                // Add other providers like Serilog, NLog, File logger here if needed
                configure.SetMinimumLevel(LogLevel.Debug); // Set minimum log level
            });

            // --- Register Services ---
            // Use Singleton for services that maintain state or are expensive to create.
            // Use Transient if a new instance is needed every time it's requested.
            // Use Scoped for web apps (less common in simple WPF unless using specific patterns).

            // Display Services
            services.AddSingleton<IDisplayInfoService, DisplayInfoService>();
            services.AddSingleton<IDisplayConfigService, DisplayConfigService>();
            services.AddSingleton<IDisplayScaleService, DisplayScaleService>();
            // Preset Service
            services.AddSingleton<IDisplayPresetService, DisplayPresetService>();

            // Applicator Service
            services.AddSingleton<IDisplaySettingsApplicator, DisplaySettingsApplicator>();

            // --- Register ViewModels ---
            // ViewModels can often be Transient unless they need to maintain state across navigations/windows.
            // If sub-VMs are always used *within* DisplaySettingsViewModel, they could be transient,
            // but Singleton might be simpler if only one instance of the main view exists. Let's use Singleton for simplicity here.
            services.AddSingleton<DeviceSelectorViewModel>();
            services.AddSingleton<PresetManagerViewModel>();
            services.AddSingleton<DisplaySettingsViewModel>(); // Main ViewModel

            // --- Register Views (Windows/UserControls) ---
            // Register MainWindow (usually Singleton)
            services.AddSingleton<MainWindow>();

            // DisplaySettingsView is a UserControl, typically created by XAML.
            // We don't usually register UserControls directly unless needed for specific scenarios.
            // The DataContext (DisplaySettingsViewModel) will be assigned later.

            // Register Popup window if it needs services injected (unlikely for the simple identify popup)
            // services.AddTransient<DisplayPopupWindow>();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // --- Resolve and Show MainWindow ---
            // Get the MainWindow instance from the DI container
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            // The MainWindow's constructor will now receive its dependencies (like DisplaySettingsViewModel)
            // automatically if configured correctly (see MainWindow modification below).

            mainWindow.Show();
        }
    }
}