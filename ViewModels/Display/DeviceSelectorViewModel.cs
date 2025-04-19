using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;
using BorderlessWindowApp.Views;

namespace BorderlessWindowApp.ViewModels.Display
{
    public class DeviceSelectorViewModel : INotifyPropertyChanged
    {
        private readonly IDisplayInfoService _infoService;
        private List<DisplayDeviceInfo> _allDeviceInfos = new();
        private bool _isLoading = false;

        public ObservableCollection<DisplayDeviceInfo> DisplayDevices { get; } = new();

        private DisplayDeviceInfo? _selectedDevice;
        public DisplayDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                // Note: Comparing DisplayDeviceInfo might need a proper Equals override based on path/name
                if (!Equals(_selectedDevice, value))
                {
                    _selectedDevice = value;
                    OnPropertyChanged(); // Notify that selection changed
                }
            }
        }

        // Optional: Keep Identify command here?
        public ICommand IdentifyCommand { get; }

        public DeviceSelectorViewModel(IDisplayInfoService infoService)
        {
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            IdentifyCommand = new RelayCommand(IdentifyDisplays); // Or remove if kept in main VM
            LoadDeviceList();
        }

        public void LoadDeviceList() // Keep synchronous for simplicity unless service is async
        {
            if (_isLoading) return;
            _isLoading = true;

            DisplayDevices.Clear();
            _allDeviceInfos.Clear();
            SelectedDevice = null; // Clear selection before loading

            try
            {
                _allDeviceInfos = _infoService.GetAllDisplayDevices()  ?? new List<DisplayDeviceInfo>();

                // Add only devices that seem valid
                foreach (var device in _allDeviceInfos.Where(d => !string.IsNullOrEmpty(d.FriendlyName) && !string.IsNullOrEmpty(d.DeviceName)))
                {
                    DisplayDevices.Add(device);
                }

                // Set the initial selection - This will trigger PropertyChanged
                SelectedDevice = DisplayDevices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading display devices: {ex.Message}");
                // TODO: Handle error reporting more robustly (e.g., expose an error state/message property)
            }
            finally
            {
                _isLoading = false;
            }
        }

       // --- Updated IdentifyDisplays method ---
        private void IdentifyDisplays()
        {
            Console.WriteLine("IdentifyDisplays command executed.");
            try
            {
                // Ensure we have the latest device info including layout
                // If LoadDeviceList isn't guaranteed to have run recently, call it again
                // or better, just use the cached _allDeviceInfos if it's kept up-to-date.
                // Re-fetching might be safer if layouts can change dynamically.
                var currentDevices = _infoService.GetAllDisplayDevices(); // Re-fetch for current layout
                 if (currentDevices == null || !currentDevices.Any())
                 {
                      Console.WriteLine("No display devices found to identify.");
                      // TODO: Show feedback to user
                      return;
                 }


                // Use a simple counter for the displayed number
                int displayIndex = 1;
                foreach (var device in currentDevices)
                {
                    // Ensure we have needed layout info
                    if (device.Width <= 0 || device.Height <= 0)
                    {
                        Console.WriteLine($"Skipping device '{device.FriendlyName ?? device.DeviceName}' due to missing dimensions.");
                        continue;
                    }


                    // Alternatively, use display number from DeviceName if preferred and reliable:
                    string displayNum = device.Identity; 
                    string displayText = $"{displayNum}";

                    // Create and configure the popup window
                    var popup = new DisplayPopupWindow(displayText)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual // Important!
                    };

                    // --- Calculate position to center the popup ---
                    // Define desired popup size (adjust as needed)
                    double popupWidth = 400;
                    double popupHeight = 150;

                    // Calculate center position relative to the device's screen coordinates
                    double left = device.PositionX + (device.Width / 2.0) - (popupWidth / 2.0);
                    double top = device.PositionY + (device.Height / 2.0) - (popupHeight / 2.0);

                    popup.Left = left;
                    popup.Top = top;
                    popup.Width = popupWidth;
                    popup.Height = popupHeight;
                    // --- End Position Calculation ---

                    popup.Show(); // Show the window

                    // Schedule it to close automatically after a delay
                    // Use the static helper from the popup's code-behind
                    _ = DisplayPopupWindow.CloseLater(popup, TimeSpan.FromSeconds(3));
                }
                 Console.WriteLine($"Identification popups shown for {displayIndex - 1} device(s).");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during display identification: {ex.Message}");
                // TODO: Show user feedback
            }
        }
        
        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}