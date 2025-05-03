using System.Windows;
using BorderlessWindowApp.ViewModels.Display;

namespace BorderlessWindowApp // Ensure this matches your project namespace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Inject the required ViewModel via the constructor
        public MainWindow(DisplaySettingsViewModel displayViewModel) // DI container provides this
        {
            InitializeComponent();

            // Set the DataContext for the entire window,
            // or find the specific UserControl and set its DataContext.
            // Setting it for the window often works if the UserControl is directly inside.
            this.DataContext = displayViewModel;

            // If DisplaySettingsView is named in XAML (e.g., x:Name="DisplaySettingsControl")
            // you could set it directly:
            // DisplaySettingsControl.DataContext = displayViewModel;
        }
    }
}