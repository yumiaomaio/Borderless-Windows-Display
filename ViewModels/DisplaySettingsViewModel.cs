using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media; // For Brush
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
        public ObservableCollection<string> DisplayDevices { get; } = new();
        public ObservableCollection<DisplayModeInfo> Resolutions { get; } = new();
        public ObservableCollection<int> RefreshRates { get; } = new();
        public ObservableCollection<DisplayPreset> Presets { get; set; } = new();// 添加 Presets 集合

        // --- 选中项 ---
        private string? _selectedDeviceFriendlyName;
        public string? SelectedDeviceFriendlyName { 
            get => _selectedDeviceFriendlyName;
            set
            {
                if (_selectedDeviceFriendlyName != value)
                {
                    _selectedDeviceFriendlyName = value;
                    OnPropertyChanged();
                    // 当选择的设备改变时，重新加载所有细节
                    LoadDisplayDetailsForSelectedDevice();
                }
            }
        }

        private DisplayModeInfo? _selectedResolution;
        public DisplayModeInfo? SelectedResolution { 
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
        public int SelectedRefreshRate { 
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
        public uint Dpi { 
         get => _dpi;
            set
            {
                // 可以在这里添加验证逻辑，确保 DPI 在允许范围内
                var currentDevice = GetSelectedDeviceInfo();
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
        private string? _displayName;
        public string? DisplayName { 
         get => _displayName;
            private set { _displayName = value; OnPropertyChanged(); } // 私有 set，由内部逻辑更新
        }

        private string? _displayParameters;
        public string? DisplayParameters {
            get => _displayParameters;
            private set { _displayParameters = value; OnPropertyChanged(); } // 私有 set
        }

        private uint _minDpi = 100;
        public uint MinDpi {
             get => _minDpi;
             private set { _minDpi = value; OnPropertyChanged(); }
         }

        private uint _maxDpi = 200;
        public uint MaxDpi {
             get => _maxDpi;
             private set { _maxDpi = value; OnPropertyChanged(); }
         }

        private Brush _statusColor = Brushes.Gray; // 添加 StatusColor 属性
        public Brush StatusColor
        {
            get => _statusColor;
            private set { _statusColor = value; OnPropertyChanged(); }
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
            ApplyPresetCommand = new RelayCommand(ApplyPreset, CanApplyPreset); // ApplyPreset might become async if needed
            RestoreDefaultCommand = new RelayCommand(RestoreDefault); // RestoreDefault calls sync methods
            
            _ = LoadInitialDataAsync();
            LoadDeviceList();
        }

        // 步骤 1: 加载设备列表 (仅 FriendlyName)
        private void LoadDeviceList() { 
             try
            {
                // 使用新的 GetAllDisplayDevices 获取完整信息
                _allDeviceInfos = _infoService.GetAllDisplayDevices() ?? new List<DisplayDeviceInfo>(); 

                DisplayDevices.Clear();
                foreach (var device in _allDeviceInfos.Where(d => !string.IsNullOrEmpty(d.FriendlyName))) // 确保 FriendlyName 有效 [cite: 12]
                {
                     // 使用友好的名称添加到选择列表
                    DisplayDevices.Add(device.FriendlyName!);
                }

                // 默认选择第一个设备 (如果列表不为空)
                SelectedDeviceFriendlyName = DisplayDevices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // 处理加载设备列表时的错误
                Console.WriteLine($"Error loading display devices: {ex.Message}");
                // 可以考虑设置一个错误状态或显示消息给用户
            }

             // 在加载设备后或失败时更新状态颜色
             UpdateStatusColor();
        }

        // 步骤 2: 当 SelectedDeviceFriendlyName 改变时，加载该设备的详细信息
        private void LoadDisplayDetailsForSelectedDevice() { /* ... 实现不变 ... */
             var selectedDevice = GetSelectedDeviceInfo(); // 获取当前选中设备的详细信息
            if (selectedDevice == null || selectedDevice.DeviceName == null) // 需要 DeviceName 来获取模式 [cite: 10]
            {
                // 清理旧数据或显示未选择状态
                DisplayName = "未选择设备";
                Resolutions.Clear();
                RefreshRates.Clear();
                Dpi = 100; // 重置DPI
                MinDpi = 100;
                MaxDpi = 200; // 重置范围
                UpdateDisplayParameters(); // 更新参数显示为空
                (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged(); // 更新命令状态
                return;
            }

            // 更新顶部显示的设备名称
            DisplayName = selectedDevice.FriendlyName; 

            // 加载分辨率
            Resolutions.Clear();
            var modes = _infoService.GetSupportedModes(selectedDevice.DeviceName); 
            // 使用 DistinctBy 防止重复的分辨率显示
            foreach (var mode in modes.DistinctBy(m => new { m.Width, m.Height }).OrderByDescending(m => m.Width).ThenByDescending(m => m.Height))
            {
                Resolutions.Add(mode);
            }

            // 获取并设置当前模式
            var currentMode = _infoService.GetCurrentMode(selectedDevice.DeviceName); 
            if (currentMode != null)
            {
                SelectedResolution = Resolutions.FirstOrDefault(m => m.Width == currentMode.Width && m.Height == currentMode.Height);
                // UpdateRefreshRatesForSelectedResolution 会处理刷新率
            }
            else
            {
                SelectedResolution = Resolutions.FirstOrDefault(); // 默认选择第一个分辨率
            }

             // 获取并设置当前 DPI 及范围
             try
             {
                var scalingInfo = _scaleService.GetScalingInfo(selectedDevice.AdapterId, selectedDevice.SourceId); 
                if (scalingInfo.IsInitialized)
                {
                     MinDpi = scalingInfo.Minimum; 
                     MaxDpi = scalingInfo.Maximum;
                     Dpi = scalingInfo.Current; 
                }
                 else
                 {
                     // 如果获取失败，使用默认值并可能禁用 DPI 滑块
                     MinDpi = 100;
                     MaxDpi = 100;
                     Dpi = 100;
                     Console.WriteLine($"Warning: Could not get DPI scaling info for {selectedDevice.FriendlyName}.");
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Error getting DPI scaling info for {selectedDevice.FriendlyName}: {ex.Message}");
                 MinDpi = 100; MaxDpi = 100; Dpi = 100; // Fallback
             }


            // 重要：在设置 SelectedResolution 后调用，以填充刷新率下拉列表
            UpdateRefreshRatesForSelectedResolution(currentMode?.RefreshRate); // 传入当前刷新率以便尝试选中它

            // 确保 DisplayParameters 最后更新
            UpdateDisplayParameters();

            // 触发 Apply 命令状态更新
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();

             // 在加载详情后或失败时更新状态颜色和命令状态
             UpdateStatusColor();
            (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateRefreshRatesForSelectedResolution(int? targetRefreshRate = null) { 
            var selectedDevice = GetSelectedDeviceInfo();
            if (SelectedResolution == null || selectedDevice?.DeviceName == null)
            {
                RefreshRates.Clear();
                SelectedRefreshRate = 0; // 重置
                return;
            }

            RefreshRates.Clear();
            var supportedRates = _infoService.GetSupportedModes(selectedDevice.DeviceName)
                .Where(m => m.Width == SelectedResolution.Width && m.Height == SelectedResolution.Height)
                .Select(m => m.RefreshRate)
                .Distinct()
                .OrderBy(r => r); // 排序

            foreach (var rate in supportedRates)
            {
                RefreshRates.Add(rate);
            }

            // 尝试选中目标刷新率（可能是当前模式的刷新率，或上次选中的刷新率）
            int rateToSelect = targetRefreshRate ?? _selectedRefreshRate; // 优先使用传入的，否则用当前的
            if (RefreshRates.Contains(rateToSelect))
            {
                SelectedRefreshRate = rateToSelect;
            }
            else
            {
                SelectedRefreshRate = RefreshRates.FirstOrDefault(); // 否则选择第一个可用的
            }
        }

        private void UpdateDisplayParameters() {
            string resStr = (SelectedResolution != null) ? $"{SelectedResolution.Width}x{SelectedResolution.Height}" : "N/A";
            string rateStr = (SelectedRefreshRate > 0) ? $"{SelectedRefreshRate}Hz" : "N/A";
            string dpiStr = $"{Dpi}% DPI";

            DisplayParameters = $"{resStr}, {rateStr}, {dpiStr}";
        }

        private void ApplySettings() { /* ... 实现不变 ... */
            var selectedDevice = GetSelectedDeviceInfo();
            // CanApplySettings 确保这些值不为 null
            if (selectedDevice == null || SelectedResolution == null || SelectedRefreshRate <= 0) return;

            // 应用分辨率和刷新率
            var configRequest = new DisplayConfigRequest
            {
                DeviceName = selectedDevice.DeviceName!, // DeviceName (e.g., \\.\DISPLAY1) is needed by config service
                Width = SelectedResolution.Width,
                Height = SelectedResolution.Height,
                RefreshRate = SelectedRefreshRate
            };
            bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);
            // 应用 DPI (如果配置成功或无论如何都尝试)
            bool scaleApplied = false;
            // 只有当 DPI 不同于当前设置时才应用，避免不必要的调用
             try
             {
                 var currentScaling = _scaleService.GetScalingInfo(selectedDevice.AdapterId, selectedDevice.SourceId);
                 if (currentScaling.Current != Dpi)
                 {
                     scaleApplied = _scaleService.SetScaling(selectedDevice.AdapterId, selectedDevice.SourceId, Dpi);
                 } else {
                     scaleApplied = true; // DPI 没变，也算成功
                 }
             }
             catch(Exception ex)
             {
                 Console.WriteLine($"Error applying scaling: {ex.Message}");
                 // 可能需要通知用户缩放设置失败
             }


            // 可选: 应用成功后重新加载当前状态
            if (configApplied || scaleApplied) // 如果任一设置成功了
            {
                 // 短暂延迟后刷新，给系统一点时间反应
                 System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
                 {
                     App.Current.Dispatcher.Invoke(() => // 确保在 UI 线程执行
                     {
                         LoadDisplayDetailsForSelectedDevice();
                     });
                 });
            }
            else
            {
                // 通知用户设置失败
                 Console.WriteLine("Failed to apply display settings.");
                 // 可以显示 MessageBox 或更新状态栏
            }
            UpdateStatusColor(); // 可能需要更复杂的逻辑判断是否成功
        }

        private bool CanApplySettings() { 
        // 必须选择了有效的设备、分辨率和刷新率
             return GetSelectedDeviceInfo() != null &&
                    SelectedResolution != null &&
                    SelectedRefreshRate > 0;

        }

        private void IdentifyDisplays() { 
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
                 Console.WriteLine("IdentifyDisplays (TestDisplayTargets) is not implemented in the current DisplayInfoService.");
                 // 可以禁用按钮或通知用户
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error identifying displays: {ex.Message}");
            }
        }

        private DisplayDeviceInfo? GetSelectedDeviceInfo() {
         if (string.IsNullOrEmpty(SelectedDeviceFriendlyName))
                return null;

            return _allDeviceInfos.FirstOrDefault(d => d.FriendlyName == SelectedDeviceFriendlyName);
        }

        // --- 新增方法和逻辑 ---

        private void UpdateStatusColor()
        {
            // 根据当前状态设置颜色 (示例逻辑)
            var selectedDevice = GetSelectedDeviceInfo();
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
            LoadDeviceList();        // Then load devices (which sets the first device and triggers detail loading)
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

        private bool CanDeletePreset() { return SelectedPreset != null; }
        private async Task DeletePresetAsync()
        {
            // ... (Implementation remains the same as previous version, using _presetService.SavePresetsAsync) ...
            if (SelectedPreset == null) return;
            var presetToRemove = SelectedPreset;
            Presets.Remove(presetToRemove);
            SelectedPreset = null;
            try {
                await _presetService.SavePresetsAsync(Presets);
                Console.WriteLine($"Preset deleted: {presetToRemove.Name}");
            } catch (Exception ex) {
                Console.WriteLine($"Error saving presets after deletion: {ex.Message}");
                // Optional: Re-add to UI if save failed?
                Presets.Add(presetToRemove); // Rollback example
            }
        }

        // Apply Preset: Now applies to the CURRENTLY SELECTED device, checking compatibility
        private bool CanApplyPreset()
        {
            // Can apply if a preset is selected AND a device is selected in the UI
            return SelectedPreset != null && GetSelectedDeviceInfo() != null;
        }

        private void ApplyPreset()
        {
            if (SelectedPreset == null) return;
            var currentDevice = GetSelectedDeviceInfo();
            if (currentDevice?.DeviceName == null)
            {
                 Console.WriteLine("Cannot apply preset: No device selected in the UI.");
                 // TODO: Notify user
                 return;
            }

            Console.WriteLine($"Attempting to apply preset '{SelectedPreset.Name}' ({SelectedPreset.Parameters}) to device '{currentDevice.FriendlyName}'...");

            // 1. Check if the selected device supports the preset's Resolution
            var supportedModes = _infoService.GetSupportedModes(currentDevice.DeviceName).ToList();
            var modesWithTargetResolution = supportedModes
                .Where(m => m.Width == SelectedPreset.Width && m.Height == SelectedPreset.Height)
                .ToList();

            if (!modesWithTargetResolution.Any())
            {
                Console.WriteLine($"Error: Device '{currentDevice.FriendlyName}' does not support resolution {SelectedPreset.Width}x{SelectedPreset.Height}.");
                // TODO: Notify User
                return;
            }

            // 2. Determine the Target Refresh Rate
            int targetRefreshRate = SelectedPreset.RefreshRate;
            bool rateSupported = modesWithTargetResolution.Any(m => m.RefreshRate == targetRefreshRate);

            if (!rateSupported)
            {
                // Exact rate not supported, find the closest or default available rate for this resolution
                int bestAvailableRate = modesWithTargetResolution
                                        .OrderBy(m => Math.Abs(m.RefreshRate - targetRefreshRate)) // Closest rate
                                        // .OrderByDescending(m => m.RefreshRate) // Or Highest rate
                                        .First() // Should always find one since modesWithTargetResolution is not empty
                                        .RefreshRate;

                Console.WriteLine($"Warning: Preset refresh rate {targetRefreshRate}Hz not supported by '{currentDevice.FriendlyName}' at {SelectedPreset.Width}x{SelectedPreset.Height}. Applying closest rate: {bestAvailableRate}Hz.");
                // TODO: Optionally notify the user about the adjustment
                targetRefreshRate = bestAvailableRate; // Use the supported rate
            }

            // 3. Apply the (potentially adjusted) settings
            Console.WriteLine($"Applying: {SelectedPreset.Width}x{SelectedPreset.Height}, {targetRefreshRate}Hz, {SelectedPreset.Dpi}% DPI");

            var configRequest = new DisplayConfigRequest
            {
                DeviceName = currentDevice.DeviceName,
                Width = SelectedPreset.Width,
                Height = SelectedPreset.Height,
                RefreshRate = targetRefreshRate // Use the determined supported rate
            };
            bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);

            bool scaleApplied = false;
             try {
                 // Only apply scaling if DPI is different from current scaling of target device
                 var currentScaling = _scaleService.GetScalingInfo(currentDevice.AdapterId, currentDevice.SourceId);
                 if (currentScaling.IsInitialized && currentScaling.Current != SelectedPreset.Dpi)
                 {
                     scaleApplied = _scaleService.SetScaling(currentDevice.AdapterId, currentDevice.SourceId, SelectedPreset.Dpi);
                 } else if (!currentScaling.IsInitialized) {
                     Console.WriteLine("Warning: Could not get current scaling info. Attempting to set DPI anyway.");
                     scaleApplied = _scaleService.SetScaling(currentDevice.AdapterId, currentDevice.SourceId, SelectedPreset.Dpi);
                 } else {
                     scaleApplied = true; // DPI was already correct
                 }
             } catch (Exception ex) {
                 Console.WriteLine($"Error applying scaling from preset: {ex.Message}");
             }

            // 4. Update ViewModel state to reflect applied settings
            if (configApplied || scaleApplied)
            {
                Console.WriteLine($"Preset '{SelectedPreset.Name}' applied (potentially with adjustments).");

                // Reloading details is the most reliable way to update the UI
                // It ensures we show the *actual* state after applying.
                 LoadDisplayDetailsForSelectedDevice();

                 // Alternative (less reliable if Apply failed partially):
                 /*
                 SelectedResolution = Resolutions.FirstOrDefault(r => r.Width == SelectedPreset.Width && r.Height == SelectedPreset.Height);
                 if (SelectedResolution != null) {
                     // UpdateRefreshRates will be called by SelectedResolution setter
                      // Need to ensure the targetRefreshRate is set *after* RefreshRates list is updated
                     App.Current.Dispatcher.InvokeAsync(() => {
                         SelectedRefreshRate = targetRefreshRate; // Set the rate that was actually applied
                         Dpi = SelectedPreset.Dpi;                 // Set the DPI
                         UpdateDisplayParameters();
                     }, System.Windows.Threading.DispatcherPriority.Background);
                 }
                 */
            }
            else
            {
                 Console.WriteLine($"Failed to apply preset '{SelectedPreset.Name}'. Config Success: {configApplied}, Scaling Success: {scaleApplied}");
                 // TODO: Notify user
            }
            UpdateStatusColor();
        }
        
        private void RestoreDefault()
        {
            Console.WriteLine("RestoreDefault command executed.");
            LoadDisplayDetailsForSelectedDevice();
        }


        // --- INotifyPropertyChanged 实现 ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // ... Update command CanExecute states as before ...
            if (propertyName == nameof(SelectedDeviceFriendlyName) ||
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
            if (propertyName == nameof(SelectedDeviceFriendlyName)) // Check if device is selected for ApplyPreset
            {
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
     
}