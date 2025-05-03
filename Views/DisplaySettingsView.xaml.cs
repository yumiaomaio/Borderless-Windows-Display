using System.Windows;
using System.Windows.Controls;
using BorderlessWindowApp.Services.Display.implement;
using BorderlessWindowApp.Services.Presets;
using BorderlessWindowApp.ViewModels.Display;
using Microsoft.Extensions.Logging.Abstractions;

namespace BorderlessWindowApp.Views
{
    public partial class DisplaySettingsView : UserControl
    {

        public DisplaySettingsView() 
        {
            InitializeComponent();

            // You can keep the Loaded event if needed for UI-specific adjustments
            // that must happen after the control and potentially its DataContext are loaded.
            // this.Loaded += DisplaySettingsView_Loaded;
        }

        // Example: Keep Loaded event handler if needed, but avoid ViewModel logic here.
        private void DisplaySettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // This code runs after the control is loaded.
            // The DataContext should be set by this point if done correctly
            // by the parent window/container.

            // Example of accessing ViewModel safely if needed:
            /*
            if (this.DataContext is ViewModels.DisplaySettingsViewModel vm)
            {
                // You can interact with vm here, but it's usually better
                // to handle interactions via Commands or Bindings.
                // Example: vm.SomeMethodToRunOnLoad();
            }
            else
            {
                // Handle case where DataContext is not set or is of wrong type, if necessary.
                 System.Diagnostics.Debug.WriteLine("Warning: DataContext is not DisplaySettingsViewModel in DisplaySettingsView_Loaded.");
            }
            */
        }
    }
}