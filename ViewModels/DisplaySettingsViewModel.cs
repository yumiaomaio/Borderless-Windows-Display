using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;
using BorderlessWindowApp.Models;
using BorderlessWindowApp.Interop.Structs.Display;

namespace BorderlessWindowApp.ViewModels
{
    public class DisplaySettingsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> DeviceNames { get; } = new();
        public ObservableCollection<DisplayModeInfo> SupportedModes { get; } = new();

        private string _selectedDevice;
        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    LoadDeviceDetails(value);
                }
            }
        }

        private DisplayModeInfo _selectedMode;
        public DisplayModeInfo SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        private DpiScalingInfo _currentScaling;
        public DpiScalingInfo CurrentScaling
        {
            get => _currentScaling;
            set => SetProperty(ref _currentScaling, value);
        }

        private uint _selectedScaling;
        public uint SelectedScaling
        {
            get => _selectedScaling;
            set => SetProperty(ref _selectedScaling, value);
        }

        public ICommand ApplyConfigCommand { get; }
        public ICommand SetScalingCommand { get; }

        private readonly IDisplayInfoService _infoService;
        private readonly IDisplayConfigService _configService;
        private readonly IDisplayScaleService _scaleService;

        private LUID _adapterId;
        private uint _sourceId;

        public DisplaySettingsViewModel(
            IDisplayInfoService infoService,
            IDisplayConfigService configService,
            IDisplayScaleService scaleService)
        {
            _infoService = infoService;
            _configService = configService;
            _scaleService = scaleService;

            ApplyConfigCommand = new RelayCommand(ApplyDisplayConfiguration);
            SetScalingCommand = new RelayCommand(SetScaling);

            LoadDevices();
        }

        private void LoadDevices()
        {
            DeviceNames.Clear();
            foreach (var name in _infoService.GetAllDeviceNames())
            {
                DeviceNames.Add(name);
            }
        }

        private void LoadDeviceDetails(string deviceName)
        {
            SupportedModes.Clear();
            foreach (var mode in _infoService.GetSupportedModes(deviceName))
            {
                SupportedModes.Add(mode);
            }

            var current = _infoService.GetCurrentMode(deviceName);
            if (current != null)
                SelectedMode = current;

            if (_infoService.TryGetAdapterAndSourceId(deviceName, out _adapterId, out _sourceId))
            {
                var scaling = _scaleService.GetScalingInfo(_adapterId, _sourceId);
                if (scaling != null && scaling.IsInitialized)
                {
                    CurrentScaling = scaling;
                    SelectedScaling = scaling.Current;
                }
            }
        }

        private void ApplyDisplayConfiguration()
        {
            var request = new DisplayConfigRequest
            {
                DeviceName = SelectedDevice,
                Width = SelectedMode.Width,
                Height = SelectedMode.Height,
                RefreshRate = SelectedMode.RefreshRate,
                PositionX = 0,
                PositionY = 0
            };

            _configService.ApplyDisplayConfiguration(request);
        }

        private void SetScaling()
        {
            _scaleService.SetScaling(_adapterId, _sourceId, SelectedScaling);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
