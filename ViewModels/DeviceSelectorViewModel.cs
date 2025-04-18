using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.ViewModels
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
                _allDeviceInfos = _infoService.GetAllDisplayDevices() ?? new List<DisplayDeviceInfo>();

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

        // Optional: Keep Identify logic here?
        private void IdentifyDisplays()
        {
            try
            {
                _infoService.TestDisplayTargets();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error identifying displays: {ex.Message}");
                // TODO: Handle error reporting
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