using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Views;

namespace BorderlessWindowApp
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IDisplayInfoService, DisplayInfoService>();
            services.AddSingleton<IDisplayConfigService, DisplayConfigService>();
            services.AddSingleton<IDisplayScaleService, DisplayScaleService>();

            services.AddTransient<DisplaySettingsView>();

            ServiceProvider = services.BuildServiceProvider();

            var window = ServiceProvider.GetRequiredService<DisplaySettingsView>();
            window.Show();
        }
    }
}