using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media; // For Brush
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;
using BorderlessWindowApp.Interop.Structs.Display; // For LUID

namespace BorderlessWindowApp.ViewModels
{
    // 定义预设项的模型
    public class DisplayPreset : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _parameters = ""; // e.g., "1920x1080, 60Hz, 100% DPI"
        public string Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        // 可以添加更多属性来存储实际的设置值
        public string TargetDeviceName { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public uint Dpi { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString() => Name; // For potential debugging or simple display
    }


    public class DisplaySettingsViewModel : INotifyPropertyChanged
    {
        // --- 服务 ---
        private readonly IDisplayInfoService _infoService;
        private readonly IDisplayConfigService _configService;
        private readonly IDisplayScaleService _scaleService;

        // --- 数据集合 ---
        private List<DisplayDeviceInfo> _allDeviceInfos = new();
        public ObservableCollection<string> DisplayDevices { get; } = new();
        public ObservableCollection<DisplayModeInfo> Resolutions { get; } = new();
        public ObservableCollection<int> RefreshRates { get; } = new();
        public ObservableCollection<DisplayPreset> Presets { get; set; } = new(); // 添加 Presets 集合

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
            IDisplayScaleService scaleService)
        {
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));

            // 初始化命令
            ApplyCommand = new RelayCommand(ApplySettings, CanApplySettings);
            IdentifyCommand = new RelayCommand(IdentifyDisplays);
            SavePresetCommand = new RelayCommand(SavePreset, CanSavePreset); // 实现 SavePreset 和 CanSavePreset
            DeletePresetCommand = new RelayCommand(DeletePreset, CanDeletePreset); // 实现 DeletePreset 和 CanDeletePreset
            ApplyPresetCommand = new RelayCommand(ApplyPreset, CanApplyPreset); // 实现 ApplyPreset 和 CanApplyPreset
            RestoreDefaultCommand = new RelayCommand(RestoreDefault); // 实现 RestoreDefault

            LoadDeviceList();
            LoadPresets(); // 加载已保存的预设
        }

        // Design-time constructor (optional, for XAML preview)
        public DisplaySettingsViewModel()
        {
            // Initialize with dummy data for XAML designer
            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                _infoService = null!; // Or mock services
                _configService = null!;
                _scaleService = null!;
                DisplayDevices.Add("设计时显示器 1");
                DisplayDevices.Add("设计时显示器 2");
                SelectedDeviceFriendlyName = "设计时显示器 1";
                Resolutions.Add(new DisplayModeInfo { Width = 1920, Height = 1080, RefreshRate = 60 });
                SelectedResolution = Resolutions[0];
                RefreshRates.Add(60);
                SelectedRefreshRate = 60;
                Dpi = 125; MinDpi = 100; MaxDpi = 200;
                DisplayName = "设计时显示器 1";
                DisplayParameters = "1920x1080, 60Hz, 125% DPI";
                StatusColor = Brushes.LimeGreen;
                Presets.Add(new DisplayPreset { Name = "游戏模式", Parameters = "1920x1080, 144Hz, 100%" });
                Presets.Add(new DisplayPreset { Name = "工作模式", Parameters = "2560x1440, 60Hz, 125%" });
                ApplyCommand = new RelayCommand(() => { }, () => true);
                IdentifyCommand = new RelayCommand(() => { });
                SavePresetCommand = new RelayCommand(() => { }, () => true);
                DeletePresetCommand = new RelayCommand(() => { }, () => Presets.Any());
                ApplyPresetCommand = new RelayCommand(() => { }, () => Presets.Any());
                RestoreDefaultCommand = new RelayCommand(() => { });
            }
            else
            {
                 throw new InvalidOperationException("Default constructor is only for design time.");
            }
        }


        // --- 方法 (部分已在之前代码中) ---        // 步骤 1: 加载设备列表 (仅 FriendlyName)

        private void LoadDeviceList() { /* ... 实现不变 ... */
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
        private void LoadPresets()
        {
            Presets.Clear();
            // TODO: 从持久化存储（如文件、注册表、数据库）加载预设列表
            // 示例:
            // var loadedPresets = PresetService.Load();
            // foreach(var p in loadedPresets) Presets.Add(p);

            // 临时添加示例数据
             if (!DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) // Don't add dummy data if already added by design-time ctor
             {
                 Presets.Add(new DisplayPreset { Name="演示1", Parameters="1920x1080, 60Hz, 100%", TargetDeviceName="Monitor A", Width=1920, Height=1080, RefreshRate=60, Dpi=100 });
                 Presets.Add(new DisplayPreset { Name="演示2", Parameters="2560x1440, 120Hz, 125%", TargetDeviceName="Monitor B", Width=2560, Height=1440, RefreshRate=120, Dpi=125 });
             }
             SelectedPreset = null; // 默认不选中任何预设
        }

        private bool CanSavePreset()
        {
            // 必须选择了有效的设备、分辨率、刷新率才能保存
            return GetSelectedDeviceInfo() != null &&
                   SelectedResolution != null &&
                   SelectedRefreshRate > 0;
        }

        private void SavePreset()
        {
            // TODO: 实现保存逻辑
            // 1. 获取当前设置
            var currentDevice = GetSelectedDeviceInfo();
            if (currentDevice == null || SelectedResolution == null || SelectedRefreshRate <= 0) return;

            // 2. (可选) 弹出对话框让用户命名预设
            string presetName = $"预设 {Presets.Count + 1}"; // 简单命名

            // 3. 创建 Preset 对象
            var newPreset = new DisplayPreset
            {
                Name = presetName,
                Parameters = $"{SelectedResolution.Width}x{SelectedResolution.Height}, {SelectedRefreshRate}Hz, {Dpi}% DPI",
                TargetDeviceName = currentDevice.FriendlyName ?? "未知设备", // 保存友好名称供参考
                // 保存实际值用于应用
                Width = SelectedResolution.Width,
                Height = SelectedResolution.Height,
                RefreshRate = SelectedRefreshRate,
                Dpi = Dpi
            };

            // 4. 添加到集合并持久化
            Presets.Add(newPreset);
            // TODO: PresetService.Save(Presets);

            Console.WriteLine($"Preset saved: {newPreset.Name}");
        }

        private bool CanDeletePreset()
        {
            // 必须选中了一个预设才能删除
            return SelectedPreset != null;
        }

        private void DeletePreset()
        {
            if (SelectedPreset == null) return;

            // TODO: (可选) 确认对话框

            var presetToRemove = SelectedPreset;
            Presets.Remove(presetToRemove);
            // TODO: PresetService.Save(Presets); // 更新持久化存储

            Console.WriteLine($"Preset deleted: {presetToRemove.Name}");
            SelectedPreset = null; // 清除选中状态
        }

        private bool CanApplyPreset()
        {
            // 必须选中了一个预设才能应用
            return SelectedPreset != null;
        }

        private void ApplyPreset()
        {
            if (SelectedPreset == null) return;

            // TODO: 实现应用预设的逻辑
            // 1. 找到预设对应的设备 (可能需要根据 TargetDeviceName 查找)
            //    注意：设备名称可能改变，需要更健壮的匹配逻辑 (例如设备路径或 ID)
            //    这里简化为应用到当前选中的设备
            var targetDevice = GetSelectedDeviceInfo();
            if (targetDevice == null)
            {
                 Console.WriteLine("Cannot apply preset: No device selected.");
                 // 或者尝试根据 SelectedPreset.TargetDeviceName 查找
                 targetDevice = _allDeviceInfos.FirstOrDefault(d => d.FriendlyName == SelectedPreset.TargetDeviceName);
                 if (targetDevice == null || targetDevice.DeviceName == null) {
                     Console.WriteLine($"Cannot apply preset: Target device '{SelectedPreset.TargetDeviceName}' not found or invalid.");
                     return; // 无法找到目标设备
                 }
                 // 如果找到了，可能需要更新 UI 上的 SelectedDeviceFriendlyName
                 // SelectedDeviceFriendlyName = targetDevice.FriendlyName; // 这会触发重新加载，可能不是期望行为
            }


            Console.WriteLine($"Applying preset '{SelectedPreset.Name}' to device '{targetDevice.FriendlyName}'...");

            // 2. 应用配置 (类似 ApplySettings，但使用预设的值)
             var configRequest = new DisplayConfigRequest
            {
                DeviceName = targetDevice.DeviceName!,
                Width = SelectedPreset.Width,
                Height = SelectedPreset.Height,
                RefreshRate = SelectedPreset.RefreshRate
            };
            bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);

             // 3. 应用 DPI
             bool scaleApplied = false;
             try
             {
                scaleApplied = _scaleService.SetScaling(targetDevice.AdapterId, targetDevice.SourceId, SelectedPreset.Dpi);
             }
             catch (Exception ex)
             {
                  Console.WriteLine($"Error applying scaling from preset: {ex.Message}");
             }


            // 4. (可选) 应用后更新 ViewModel 状态以反映预设值
            if (configApplied || scaleApplied)
            {
                 // 如果应用到了当前选中的设备，则更新 UI
                 if (targetDevice.FriendlyName == SelectedDeviceFriendlyName)
                 {
                      // 更新 ViewModel 属性，这会通过绑定更新 UI
                      // 需要注意，这可能会触发连锁更新（如刷新率列表）
                      SelectedResolution = Resolutions.FirstOrDefault(r => r.Width == SelectedPreset.Width && r.Height == SelectedPreset.Height);
                      if (SelectedResolution != null) // 确保分辨率有效
                      {
                         // 在设置分辨率后，刷新率列表会更新，然后设置预设的刷新率
                         // UpdateRefreshRates 会被 SelectedResolution setter 触发
                          Dpi = SelectedPreset.Dpi; // 设置 DPI
                          // 等待刷新率列表更新后再设置
                          App.Current.Dispatcher.InvokeAsync(() => {
                              SelectedRefreshRate = SelectedPreset.RefreshRate;
                              UpdateDisplayParameters(); // 确保最终参数正确显示
                          }, System.Windows.Threading.DispatcherPriority.Background);
                      } else {
                           Console.WriteLine("Warning: Preset resolution not found in supported modes for the current device.");
                           LoadDisplayDetailsForSelectedDevice(); // Re-load current actual settings
                      }
                 }
                 Console.WriteLine($"Preset '{SelectedPreset.Name}' applied.");
                 UpdateStatusColor();
            }
             else
             {
                 Console.WriteLine($"Failed to apply preset '{SelectedPreset.Name}'.");
                 // 可能需要通知用户
             }
        }

        private void RestoreDefault()
        {
            // TODO: 实现恢复默认设置的逻辑
            // 这可能涉及调用系统 API 或应用一个“默认”的预设配置
            Console.WriteLine("RestoreDefault command executed (implementation pending).");

            // 示例：重新加载当前设备的实际设置，覆盖用户未应用的更改
            LoadDisplayDetailsForSelectedDevice();
        }

        // --- INotifyPropertyChanged 实现 ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // 触发依赖命令的状态更新
            if (propertyName == nameof(SelectedDeviceFriendlyName) ||
                propertyName == nameof(SelectedResolution) ||
                propertyName == nameof(SelectedRefreshRate))
            {
                (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged(); // 保存命令依赖当前设置
            }

             if (propertyName == nameof(SelectedPreset))
             {
                 (DeletePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                 (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
             }
        }

        // --- RelayCommand 类 ---
        public class RelayCommand : ICommand {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute; // 使 canExecute 可选

            public event EventHandler? CanExecuteChanged; // C# 6 null-conditional operator

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            // 如果未提供 canExecute 函数，则始终认为可以执行
            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

            public void Execute(object? parameter) => _execute();

            // 公开方法以允许 ViewModel 手动触发 CanExecuteChanged
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

     // --- Value Converters (示例) ---
     // 需要在你的项目中实际创建这个类
     public class DisplayModeInfoToStringConverter : System.Windows.Data.IValueConverter
     {
         public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
         {
             if (value is DisplayModeInfo mode)
             {
                 //return $"{mode.Width} x {mode.Height}"; // 只显示分辨率
                 return mode.ToString(); // 使用模型自带的 ToString()，例如 "1920x1080@60Hz"
             }
             return Binding.DoNothing;
         }

         public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
         {
             throw new NotImplementedException(); // 通常 ComboBox 不需要 ConvertBack
         }
     }
}