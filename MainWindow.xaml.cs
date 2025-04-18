using System.Windows;

namespace BorderlessWindowApp // Ensure this matches your project namespace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); 
            // MainWindow now only needs to initialize itself.
            // The UserControl handles its own data loading.
        }
    }
}