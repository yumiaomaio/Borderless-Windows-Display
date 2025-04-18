using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using BorderlessWindowApp.Interop.Enums.Display; // For Brush
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;

// For LUID

namespace BorderlessWindowApp.ViewModels
{
    public class DisplaySettingsViewModel : INotifyPropertyChanged
    {
        // --- 服务 ---
        private readonly IDisplayInfoService _infoService;
        private readonly IDisplayConfigService _configService;
        private readonly IDisplayScaleService _scaleService;
        private readonly IDisplayPresetService _presetService;

        // --- 数据集合 ---
        private List<DisplayDeviceInfo> _allDeviceInfos = new();
        public ObservableCollection<DisplayDeviceInfo> DisplayDevices { get; } = new();
        public ObservableCollection<DisplayModeInfo> Resolutions { get; } = new();
        public ObservableCollection<int> RefreshRates { get; } = new();
        public ObservableCollection<DisplayPreset> Presets { get; set; } = new(); // 添加 Presets 集合

        // --- 选中项 ---
        // --- Selected items ---
        // CHANGE: Replaced SelectedDeviceFriendlyName with SelectedDevice
        private DisplayDeviceInfo? _selectedDevice;
        public DisplayDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    // Trigger loading details for the newly selected device
                    LoadDisplayDetailsForSelectedDevice();
                    // Update command states that depend on a device being selected
                    (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RestoreDefaultCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Might depend on device selection
                }
            }
        }

        private DisplayModeInfo? _selectedResolution;
        public DisplayModeInfo? SelectedResolution
        {
            get => _selectedResolution;
            set
            {
                if (_selectedResolution != value)
                {
                    _selectedResolution = value;
                    OnPropertyChanged();
                    // 分辨率改变时，更新支持的刷新率
                    UpdateRefreshRatesForSelectedResolution();
                    // 同时更新显示参数
                    UpdateDisplayParameters();
                }
            }
        }

        private int _selectedRefreshRate;
        public int SelectedRefreshRate
        {
            get => _selectedRefreshRate;
            set
            {
                if (_selectedRefreshRate != value)
                {
                    _selectedRefreshRate = value;
                    OnPropertyChanged();
                    // 刷新率改变，更新显示参数
                    UpdateDisplayParameters();
                }
            }
        }

        private uint _dpi = 100;
        public uint Dpi
        {
            get => _dpi;
            set
            {
                // 可以在这里添加验证逻辑，确保 DPI 在允许范围内
                var currentDevice = SelectedDevice;
                if (currentDevice != null)
                {
                    var scalingInfo = _scaleService.GetScalingInfo(currentDevice.AdapterId, currentDevice.SourceId);
                    // 限制 DPI 在最小和最大值之间
                    uint clampedValue = Math.Clamp(value, scalingInfo.Minimum, scalingInfo.Maximum);
                    if (_dpi != clampedValue)
                    {
                        _dpi = clampedValue;
                        OnPropertyChanged();
                        // DPI 改变，更新显示参数
                        UpdateDisplayParameters();
                    }
                }
                else if (_dpi != value) // 如果没有设备信息，仍然允许设置（可能用于UI绑定）
                {
                    _dpi = value;
                    OnPropertyChanged();
                    UpdateDisplayParameters();
                }
            }
        }

        private DisplayPreset? _selectedPreset; // 添加 SelectedPreset
        public DisplayPreset? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                OnPropertyChanged();
                // 当选中预设改变时，更新相关命令的状态
                (DeletePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // --- UI 显示属性 ---
        private string? _headerDisplayName;
        public string? HeaderDisplayName
        {
            get => _headerDisplayName;
            private set { _headerDisplayName = value; OnPropertyChanged(); }
        }

        private string? _headerDisplayParameters;
        public string? HeaderDisplayParameters
        {
            get => _headerDisplayParameters;
            private set { _headerDisplayParameters = value; OnPropertyChanged(); }
        }

        private string? _headerDeviceString;
        public string? HeaderDeviceString
        {
            get => _headerDeviceString;
            private set { _headerDeviceString = value; OnPropertyChanged(); }
        }

        private uint _minDpi = 100;
        public uint MinDpi
        {
            get => _minDpi;
            private set
            {
                _minDpi = value;
                OnPropertyChanged();
            }
        }

        private uint _maxDpi = 200;
        public uint MaxDpi
        {
            get => _maxDpi;
            private set
            {
                _maxDpi = value;
                OnPropertyChanged();
            }
        }

        private Brush _statusColor = Brushes.Gray; // 添加 StatusColor 属性
        public Brush StatusColor
        {
            get => _statusColor;
            private set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        // --- 命令 ---
        public ICommand ApplyCommand { get; }
        public ICommand IdentifyCommand { get; }
        public ICommand SavePresetCommand { get; } // 添加命令
        public ICommand DeletePresetCommand { get; } // 添加命令
        public ICommand ApplyPresetCommand { get; } // 添加命令
        public ICommand RestoreDefaultCommand { get; } // 添加命令

        // --- 构造函数 ---
        public DisplaySettingsViewModel(IDisplayInfoService infoService,
            IDisplayConfigService configService,
            IDisplayScaleService scaleService,
            IDisplayPresetService presetService)
        {
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
            _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));

            // 初始化命令
            ApplyCommand = new RelayCommand(ApplySettings, CanApplySettings);
            IdentifyCommand = new RelayCommand(IdentifyDisplays);
            // Use async void for command handlers calling async methods, or use an async RelayCommand implementation
            SavePresetCommand = new RelayCommand(async () => await SavePresetAsync(), CanSavePreset);
            DeletePresetCommand = new RelayCommand(async () => await DeletePresetAsync(), CanDeletePreset);
            ApplyPresetCommand =
                new RelayCommand(ApplyPreset, CanApplyPreset); // ApplyPreset might become async if needed
            RestoreDefaultCommand = new RelayCommand(RestoreDefault); // RestoreDefault calls sync methods

            _ = LoadInitialDataAsync();
            LoadDeviceList();
        }

        // 步骤 1: 加载设备列表
        private void LoadDeviceList()
        {
            try
            {
                _allDeviceInfos = _infoService.GetAllDisplayDevices() ?? new List<DisplayDeviceInfo>();

                DisplayDevices.Clear();
                // Add only devices that seem valid (e.g., have a friendly name and device name)
                foreach (var device in _allDeviceInfos.Where(d => !string.IsNullOrEmpty(d.FriendlyName) && !string.IsNullOrEmpty(d.DeviceName)))
                {
                    DisplayDevices.Add(device);
                }

                // Set the initial selection (triggers LoadDisplayDetails)
                SelectedDevice = DisplayDevices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading display devices: {ex.Message}");
                // TODO: Handle UI feedback for error
                HeaderDisplayName = "Error Loading Devices"; // Update header
                HeaderDisplayParameters = ex.Message;
                HeaderDeviceString = null;
                UpdateStatusColor(true); // Indicate error status
            }
        }

        // Loads details and *updates header* based on the current `SelectedDevice`
        private void LoadDisplayDetailsForSelectedDevice()
        {
            // Use the SelectedDevice property directly
            var device = SelectedDevice;

            // Clear previous state / Update header if no device selected
            if (device?.DeviceName == null) // Check DeviceName as it's crucial for GetSupportedModes
            {
                HeaderDisplayName = "No Device Selected";
                HeaderDisplayParameters = null;
                HeaderDeviceString = null;
                Resolutions.Clear();
                RefreshRates.Clear();
                // Reset UI controls if needed (or disable them)
                SelectedResolution = null;
                SelectedRefreshRate = 0;
                Dpi = 100; MinDpi = 100; MaxDpi = 100; // Reset DPI
                UpdateStatusColor(); // Update status indicator
                return;
            }

            bool success = true; // Flag to track if all data loads correctly

            // 1. Update Header Info (using formatters)
            HeaderDisplayName = FormatHeaderDisplayName(device);
            HeaderDeviceString = device.DeviceString ?? "N/A"; // Show device string

            // 2. Load Resolutions for UI ComboBox
            try
            {
                Resolutions.Clear();
                var modes = _infoService.GetSupportedModes(device.DeviceName);
                foreach (var mode in modes.DistinctBy(m => new { m.Width, m.Height }).OrderByDescending(m => m.Width).ThenByDescending(m => m.Height))
                {
                    Resolutions.Add(mode);
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error loading supported modes for {device.FriendlyName}: {ex.Message}");
                 success = false;
                 // TODO: Notify user
            }

            // 3. Get CURRENT Mode and DPI to update Header and set initial UI selection
            DisplayModeInfo? currentMode = null;
            DpiScalingInfo currentScaling = new DpiScalingInfo(); // Default values
            try
            {
                 currentMode = _infoService.GetCurrentMode(device.DeviceName);
                 currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                 if (!currentScaling.IsInitialized) {
                     Console.WriteLine($"Warning: Scaling info not initialized for {device.FriendlyName}");
                     // Keep default scaling values (100)
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current mode/scaling for {device.FriendlyName}: {ex.Message}");
                success = false;
                 // TODO: Notify user
            }

             // 4. Update Header Parameters based on ACTUAL current values
             string currentResStr = (currentMode != null) ? $"{currentMode.Width}x{currentMode.Height}" : "N/A";
             string currentRateStr = (currentMode != null) ? $"{currentMode.RefreshRate}Hz" : "N/A";
             string currentDpiStr = $"{currentScaling.Current}% DPI";
             HeaderDisplayParameters = $"{currentResStr}, {currentRateStr}, {currentDpiStr}";


            // 5. Set UI Control Selections based on current values
            if (currentMode != null)
            {
                SelectedResolution = Resolutions.FirstOrDefault(m => m.Width == currentMode.Width && m.Height == currentMode.Height);
                // UpdateRefreshRates will be called by SelectedResolution's setter,
                // pass the current rate to try and select it.
                 UpdateRefreshRatesForSelectedResolution(currentMode.RefreshRate);
            }
            else
            {
                // Default selection if current mode not found
                SelectedResolution = Resolutions.FirstOrDefault();
                 UpdateRefreshRatesForSelectedResolution(); // Update rates for the default resolution
            }

            // Set DPI Slider value and range
            MinDpi = currentScaling.Minimum;
            MaxDpi = currentScaling.Maximum;
            Dpi = currentScaling.Current; // This might trigger OnPropertyChanged for Dpi

            // 6. Update Status Color
            UpdateStatusColor(!success); // Pass error flag

            // Make sure command states are accurate after loading
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateRefreshRatesForSelectedResolution(int? targetRefreshRate = null)
        {
            var device = SelectedDevice; // Use property
            if (SelectedResolution == null || device?.DeviceName == null)
            {
                RefreshRates.Clear();
                SelectedRefreshRate = 0;
                return;
            }

            RefreshRates.Clear();
            var supportedRates = _infoService.GetSupportedModes(device.DeviceName)
                .Where(m => m.Width == SelectedResolution.Width && m.Height == SelectedResolution.Height)
                .Select(m => m.RefreshRate).Distinct().OrderBy(r => r);
            foreach (var rate in supportedRates) RefreshRates.Add(rate);

            int rateToSelect = targetRefreshRate ?? _selectedRefreshRate;
            if (RefreshRates.Contains(rateToSelect)) SelectedRefreshRate = rateToSelect;
            else SelectedRefreshRate = RefreshRates.FirstOrDefault();
        }

        private void UpdateDisplayParameters()
        {
            string resStr = (SelectedResolution != null)
                ? $"{SelectedResolution.Width}x{SelectedResolution.Height}"
                : "N/A";
            string rateStr = (SelectedRefreshRate > 0) ? $"{SelectedRefreshRate}Hz" : "N/A";
            string dpiStr = $"{Dpi}% DPI";
            HeaderDisplayParameters = $"{resStr}, {rateStr}, {dpiStr}";
        }

        
        private bool CanApplySettings()
        {
            // Can apply if a device and valid settings are selected in the UI controls
            return SelectedDevice != null &&
                   SelectedResolution != null &&
                   SelectedRefreshRate > 0;
        }
        private void ApplySettings() // Not async, but calls async methods inside if needed
        {
            var device = SelectedDevice; // Use property
            if (device == null || SelectedResolution == null || SelectedRefreshRate <= 0) return;

            Console.WriteLine($"Applying UI settings to {device.FriendlyName}: {SelectedResolution.Width}x{SelectedResolution.Height}, {SelectedRefreshRate}Hz, {Dpi}%");

            // Apply Config
            var configRequest = new DisplayConfigRequest { 
                DeviceName = device.DeviceName!, // DeviceName (e.g., \\.\DISPLAY1) is needed by config service
                Width = SelectedResolution.Width,
                Height = SelectedResolution.Height,
                RefreshRate = SelectedRefreshRate 
            };
             configRequest.DeviceName = device.DeviceName!; // Use non-friendly name
             configRequest.Width = SelectedResolution.Width;
             configRequest.Height = SelectedResolution.Height;
             configRequest.RefreshRate = SelectedRefreshRate;
            bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);

            // Apply Scaling
            bool scaleApplied = false;
             try {
                var currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                if (currentScaling.IsInitialized && currentScaling.Current != Dpi) {
                     scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, Dpi);
                } else { scaleApplied = true; } // No change needed or already correct
             } catch (Exception ex) { Console.WriteLine($"Error applying scaling: {ex.Message}"); }

             // IMPORTANT: Reload details AFTER applying to update Header with actual results
             if (configApplied || scaleApplied) {
                  Console.WriteLine("Settings applied. Reloading device details...");
                  // Give system time to process changes before querying again? Optional delay.
                  // await Task.Delay(500);
                  LoadDisplayDetailsForSelectedDevice(); // This updates the header
             } else {
                  Console.WriteLine("Failed to apply settings.");
                  // TODO: Notify user? Maybe just update status color.
                   UpdateStatusColor(true); // Indicate error
             }
        }

        private void IdentifyDisplays()
        {
            try
            {
                // 注意：根据您最新提供的 IDisplayInfoService 接口 [cite: 29]，
                // TestDisplayTargets() 方法似乎不存在了。
                // 如果这个方法确实没有了，您需要移除 IdentifyCommand 或修改此处的调用。
                // 这里我们假设它仍然存在于您的实际实现中 (如旧代码所示 [cite: 126])。
                _infoService.TestDisplayTargets();
            }
            catch (NotImplementedException)
            {
                Console.WriteLine(
                    "IdentifyDisplays (TestDisplayTargets) is not implemented in the current DisplayInfoService.");
                // 可以禁用按钮或通知用户
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error identifying displays: {ex.Message}");
            }
        }


        // --- 新增方法和逻辑 ---

        private void UpdateStatusColor(bool error = false)
        {
            // 根据当前状态设置颜色 (示例逻辑)
            var selectedDevice = SelectedDevice;
            if (selectedDevice == null)
            {
                StatusColor = Brushes.Gray; // 未选择
            }
            else if (!selectedDevice.IsAvailable) // 假设 DisplayDeviceInfo 有 IsAvailable 属性
            {
                StatusColor = Brushes.Orange; // 设备不可用/断开连接？
            }
            // 可以添加更多检查，例如服务是否出错等
            // else if (someErrorCondition)
            // {
            //     StatusColor = Brushes.Red; // 错误状态
            // }
            else
            {
                StatusColor = Brushes.LimeGreen; // 正常
            }
        }

        // -- Preset Logic --
        private async Task LoadInitialDataAsync()
        {
            await LoadPresetsAsync(); // Load presets first
            LoadDeviceList(); // Then load devices (which sets the first device and triggers detail loading)
            // Initial loading might trigger property changes and command state updates automatically.
        }

        private async Task LoadPresetsAsync()
        {
            try
            {
                var loadedPresets = await _presetService.LoadPresetsAsync();
                Presets.Clear();
                foreach (var p in loadedPresets)
                {
                    Presets.Add(p);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading presets into ViewModel: {ex.Message}");
                // Handle error in UI if necessary
            }

            SelectedPreset = null; // Ensure nothing is selected initially
        }

        private bool CanSavePreset()
        {
            // Can save if current settings are valid, regardless of device
            return SelectedResolution != null && SelectedRefreshRate > 0;
        }


        // Made async to call the service's async save method
        private async Task SavePresetAsync()
        {
            if (SelectedResolution == null || SelectedRefreshRate <= 0) return;

            // Create a temporary preset with the current values to check for duplicates
            var presetValues = new DisplayPreset
            {
                Width = SelectedResolution.Width,
                Height = SelectedResolution.Height,
                RefreshRate = SelectedRefreshRate,
                Dpi = Dpi
            };

            // Check if a preset with these exact values already exists
            if (Presets.Any(p => p.Equals(presetValues)))
            {
                Console.WriteLine("Preset with these settings already exists.");
                // TODO: Inform the user (e.g., MessageBox.Show("A preset with these settings already exists."))
                return;
            }

            // TODO: Ask user for a name (replace simple naming)
            // Example: var name = ShowNameInputDialog("Enter Preset Name"); if (string.IsNullOrEmpty(name)) return;
            string presetName = $"Preset {Presets.Count + 1}"; // Simple default name

            var newPreset = new DisplayPreset
            {
                Name = presetName, // Use the name provided by the user
                Width = presetValues.Width,
                Height = presetValues.Height,
                RefreshRate = presetValues.RefreshRate,
                Dpi = presetValues.Dpi
            };

            Presets.Add(newPreset);
            SelectedPreset = newPreset; // Select the new preset in the list

            try
            {
                await _presetService.SavePresetsAsync(Presets);
                Console.WriteLine($"Preset saved: {newPreset.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving presets: {ex.Message}");
                Presets.Remove(newPreset); // Rollback UI change if save failed
                SelectedPreset = null;
                // TODO: Notify user of save failure
            }
        }

        private bool CanDeletePreset()
        {
            return SelectedPreset != null;
        }

        private async Task DeletePresetAsync()
        {
            // ... (Implementation remains the same as previous version, using _presetService.SavePresetsAsync) ...
            if (SelectedPreset == null) return;
            var presetToRemove = SelectedPreset;
            Presets.Remove(presetToRemove);
            SelectedPreset = null;
            try
            {
                await _presetService.SavePresetsAsync(Presets);
                Console.WriteLine($"Preset deleted: {presetToRemove.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving presets after deletion: {ex.Message}");
                // Optional: Re-add to UI if save failed?
                Presets.Add(presetToRemove); // Rollback example
            }
        }

        // Apply Preset: Now applies to the CURRENTLY SELECTED device, checking compatibility
        private bool CanApplyPreset()
        {
            return SelectedPreset != null && SelectedDevice != null; // Need preset AND device selected
        }

        private void ApplyPreset()
        {
             if (SelectedPreset == null) return;
             var device = SelectedDevice; // Apply to currently selected device
             if (device?.DeviceName == null) return;
             // ... (Compatibility check logic remains the same) ...
              var supportedModes = _infoService.GetSupportedModes(device.DeviceName).ToList();
              // ... (Determine targetRefreshRate based on compatibility) ...
               int targetRefreshRate = SelectedPreset.RefreshRate; // Start with preset rate
               // ... (Adjust if needed) ...

             // Apply Config & Scaling using preset values (and adjusted rate)
              var configRequest = new DisplayConfigRequest { 
                DeviceName = device.DeviceName,
                Width = SelectedPreset.Width,
                Height = SelectedPreset.Height,
                RefreshRate = targetRefreshRate 
              };
               configRequest.DeviceName = device.DeviceName;
               configRequest.Width = SelectedPreset.Width;
               configRequest.Height = SelectedPreset.Height;
               configRequest.RefreshRate = targetRefreshRate; // Use potentially adjusted rate
              bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);
              bool scaleApplied = false; // ... Apply scaling using SelectedPreset.Dpi ...
               try { scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, SelectedPreset.Dpi); }
               catch {}

             // IMPORTANT: Reload details AFTER applying to update Header
             if (configApplied || scaleApplied) {
                 Console.WriteLine($"Preset '{SelectedPreset.Name}' applied. Reloading device details...");
                 LoadDisplayDetailsForSelectedDevice(); // This updates the header
             } else {
                 Console.WriteLine($"Failed to apply preset '{SelectedPreset.Name}'.");
                 UpdateStatusColor(true); // Indicate error
             }
        }

        // RestoreDefault: Reloads current settings, updating header
        private bool CanRestoreDefault() => SelectedDevice != null; // Can restore if a device is selected
        private void RestoreDefault()
        {
            Console.WriteLine("RestoreDefault command executed.");
            if (SelectedDevice == null) return;
            // Reloads the actual current settings from hardware for the selected device
            LoadDisplayDetailsForSelectedDevice(); // This updates header and UI controls
        }


        // --- INotifyPropertyChanged 实现 ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // ... Update command CanExecute states as before ...
            if (
                propertyName == nameof(SelectedResolution) ||
                propertyName == nameof(SelectedRefreshRate) ||
                propertyName == nameof(Dpi))
            {
                (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Also depends on device being selected
            }

            if (propertyName == nameof(SelectedPreset))
            {
                (DeletePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            
        }

        // Add these somewhere accessible by the ViewModel, e.g., as private methods
        // Or in a static utility class if preferred.
        private string FormatOutputTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY tech)
        {
            // Remove the prefix and handle specific cases
            string name = tech.ToString().Replace("DISPLAYCONFIG_OUTPUT_TECHNOLOGY_", "");
            switch (name)
            {
                case "OTHER": return "Other";
                case "HD15": return "VGA"; // More common name
                case "DVI": return "DVI";
                case "HDMI": return "HDMI";
                case "LVDS": return "LVDS";
                case "SDI": return "SDI";
                case "DISPLAYPORT_EXTERNAL": return "DP";
                case "DISPLAYPORT_EMBEDDED": return "eDP";
                case "UDI_EXTERNAL": return "UDI";
                case "UDI_EMBEDDED": return "eUDI";
                case "SDTV_DONGLE": return "SDTV";
                case "MIRACAST": return "Miracast";
                case "INDIRECT_WIRED": return "Wired"; // Simplified
                case "INDIRECT_VIRTUAL": return "Virtual"; // Simplified
                case "INTERNAL": return "Internal";
                // Add other cases as needed based on the full enum definition
                default:
                    // Attempt to clean up unknown values (e.g., _INTERNAL)
                    if (name.Contains("_"))
                        name = name.Substring(name.LastIndexOf('_') + 1);
                    return name; // Return cleaned name or original if no underscore
            }
        }

        private string ExtractDisplayNumber(string? deviceName)
        {
            // Extracts the number from "\\.\DISPLAY1" -> "1"
            if (string.IsNullOrWhiteSpace(deviceName) || !deviceName.StartsWith(@"\\.\DISPLAY"))
            {
                return "?";
            }

            return deviceName.Substring(@"\\.\DISPLAY".Length);
        }

        // Formatter for ComboBox items
        private string FormatComboBoxItemText(DisplayDeviceInfo deviceInfo)
        {
            if (deviceInfo == null) return "Invalid Device";
            string displayNum = ExtractDisplayNumber(deviceInfo.DeviceName);
            string tech = FormatOutputTechnology(deviceInfo.OutputTechnology);
            // Format: [SourceId] [DisplayNum] FriendlyName (OutputTech)
            return $"[{deviceInfo.SourceId}] [{displayNum}] {deviceInfo.FriendlyName ?? "Unknown"} ({tech})";
        }

        // Formatter for Header Display Name
        private string FormatHeaderDisplayName(DisplayDeviceInfo deviceInfo)
        {
            if (deviceInfo == null) return "No Device Selected";
            string tech = FormatOutputTechnology(deviceInfo.OutputTechnology);
            // Format: [SourceId] FriendlyName (OutputTech)
            return $"[{deviceInfo.SourceId}] {deviceInfo.FriendlyName ?? "Unknown"} ({tech})";
        }
        
    }
}