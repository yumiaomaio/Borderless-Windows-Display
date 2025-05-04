// File: Views/ConfirmationDialog.xaml.cs
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging; // For DispatcherTimer

namespace BorderlessWindowApp.Views // Ensure namespace matches XAML
{
    public partial class ConfirmationDialog : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        private TimeSpan _remainingTime;

        public ConfirmationDialog(string message, TimeSpan timeout)
        {
            InitializeComponent();

            MessageTextBlock.Text = message;
            _remainingTime = timeout;
            UpdateCountdownText(); // Initial text

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // Public method for the caller to await the result
        public Task<bool> GetResultAsync()
        {
            return _tcs.Task;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start(); // Start timer when window is loaded
            this.Activate(); // Try to bring window to front
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            UpdateCountdownText();

            if (_remainingTime <= TimeSpan.Zero)
            {
                _timer.Stop();
                _logger?.LogInformation("Confirmation timeout expired."); // Assuming _logger exists if needed
                SetResultAndClose(false); // Timeout means Revert (false)
            }
        }

        private void UpdateCountdownText()
        {
            CountdownTextBlock.Text = $"Reverting in {(int)Math.Ceiling(_remainingTime.TotalSeconds)} seconds...";
        }

        private void KeepButton_Click(object sender, RoutedEventArgs e)
        {
             _logger?.LogInformation("User clicked Keep Settings."); // Assuming _logger exists if needed
            _timer.Stop();
            SetResultAndClose(true); // Keep means true
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
             _logger?.LogInformation("User clicked Revert Now."); // Assuming _logger exists if needed
            _timer.Stop();
            SetResultAndClose(false); // Revert means false
        }

        private void SetResultAndClose(bool result)
        {
            // TrySetResult ensures it's only set once
            _tcs.TrySetResult(result);
            this.Close();
        }

        // Optional: Handle window closing via other means (e.g., Alt+F4)
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
            // If closing manually before a choice, treat as Revert/Timeout
            _tcs.TrySetResult(false);
            base.OnClosing(e);
        }

        // Optional: Add logger field if needed for internal logging
        private static readonly Microsoft.Extensions.Logging.ILogger? _logger = null; /* TODO: Inject if needed */
    }
}