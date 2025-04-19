// File: Views/DisplayPopupWindow.xaml.cs
using System;
using System.Threading.Tasks;
using System.Windows;

namespace BorderlessWindowApp.Views // Ensure namespace matches XAML
{
    public partial class DisplayPopupWindow : Window
    {
        public DisplayPopupWindow(string displayText)
        {
            InitializeComponent();
            this.Label.Text = displayText; // Set the text passed from the constructor

            // Optional: Close on click
            this.MouseLeftButtonDown += (s, e) => this.Close();
        }

        // Static helper method to close the window after a delay
        public static async Task CloseLater(Window window, TimeSpan delay)
        {
            await Task.Delay(delay);
            // Ensure we close on the UI thread if called from background
            window.Dispatcher.Invoke(() =>
            {
                // Check if window hasn't been closed manually already
                if (window.IsLoaded)
                {
                    window.Close();
                }
            });
        }
    }
}