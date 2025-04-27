// File: ViewModels/DisplaySettingsViewModel.cs

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;

// For Formatting

namespace BorderlessWindowApp.ViewModels.Display
{
    public class DisplaySettingsViewModel : INotifyPropertyChanged
    {
        // --- Child ViewModels / Managers ---
        public DeviceSelectorViewModel DeviceSelector { get; }
        public PresetManagerViewModel PresetManager { get; }

        // --- Services ---
        private readonly IDisplayInfoService _infoService; // Still needed for loading details
        private readonly IDisplayScaleService _scaleService; // Still needed for loading details
        private readonly IDisplaySettingsApplicator _applicator; // Use the applicator service

        // --- State specific to the selected device's *details* ---
        public ObservableCollection<DisplayModeInfo> Resolutions { get; } = new();
        public ObservableCollection<int> RefreshRates { get; } = new();
        public ObservableCollection<DisplayOrientationItem> AvailableOrientations { get; } = new();
        
        private DisplayModeInfo? _selectedResolution;
        public DisplayModeInfo? SelectedResolution
        {
            get => _selectedResolution;
            set
            {
                // Use SetProperty helper for brevity and INotifyPropertyChanged
                // Pass an Action lambda to call our new update method AFTER the property changes
                SetProperty(ref _selectedResolution, value, () =>
                {
                    UpdateRefreshRatesAndCommands(); // Update refresh rates first
                    UpdateDpiRangeForResolution(value); // THEN update DPI range
                });
            }
        }
        private int _selectedRefreshRate;
        public int SelectedRefreshRate
        {
            get => _selectedRefreshRate;
            set
            {
                SetProperty(ref _selectedRefreshRate, value,
                    () => (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged());
            } // Only update Apply command state
        }
        private uint _dpi = 100;
        
        private DisplayOrientationItem? _selectedOrientation;
        public DisplayOrientationItem? SelectedOrientation
        {
            get => _selectedOrientation;
            // Update Apply command when selection changes
            set { SetProperty(ref _selectedOrientation, value, () => (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged()); }
        }
        
        public uint Dpi
        {
            get => _dpi;
            set
            {
                // Clamp against the *current* Min/Max values stored in the properties
                uint clampedValue = Math.Clamp(value, MinDpi, MaxDpi);
                SetProperty(ref _dpi, clampedValue, () => (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged());
            }
        }

        private uint _minDpi = 100;
        public uint MinDpi
        {
            get => _minDpi;
            private set => SetProperty(ref _minDpi, value);
        }

        private uint _maxDpi = 200;
        public uint MaxDpi
        {
            get => _maxDpi;
            private set => SetProperty(ref _maxDpi, value);
        }


        // --- Header Display Properties ---
        private string? _headerDisplayName;

        public string? HeaderDisplayName
        {
            get => _headerDisplayName;
            private set => SetProperty(ref _headerDisplayName, value);
        }

        private string? _headerDisplayParameters;

        public string? HeaderDisplayParameters
        {
            get => _headerDisplayParameters;
            private set => SetProperty(ref _headerDisplayParameters, value);
        }

        private string? _headerDeviceString;

        public string? HeaderDeviceString
        {
            get => _headerDeviceString;
            private set => SetProperty(ref _headerDeviceString, value);
        }

        private Brush _statusColor = Brushes.Gray;

        public Brush StatusColor
        {
            get => _statusColor;
            private set => SetProperty(ref _statusColor, value);
        }

        // --- Commands (Coordination) ---
        public ICommand ApplyCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand ApplyPresetCommand { get; }
        public ICommand RestoreDefaultCommand { get; }
        // Identify command is now likely on DeviceSelectorViewModel


        // --- Constructor ---
        public DisplaySettingsViewModel(
            DeviceSelectorViewModel deviceSelector, // Inject child VMs/Managers
            PresetManagerViewModel presetManager,
            IDisplayInfoService infoService,
            IDisplayScaleService scaleService,
            IDisplaySettingsApplicator applicator)
        {
            DeviceSelector = deviceSelector ?? throw new ArgumentNullException(nameof(deviceSelector));
            PresetManager = presetManager ?? throw new ArgumentNullException(nameof(presetManager));
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
            _applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));

            // Subscribe to device selection changes
            DeviceSelector.PropertyChanged += DeviceSelector_PropertyChanged;
            PresetManager.PropertyChanged += PresetManager_PropertyChanged;

            // Initialize Commands
            ApplyCommand = new RelayCommand(async () => await ApplySettingsAsync(), CanApplySettings);
            SavePresetCommand = new RelayCommand(async () => await SavePresetAsync(), CanSavePreset);
            ApplyPresetCommand = new RelayCommand(async () => await ApplyPresetAsync(), CanApplyPreset);
            RestoreDefaultCommand = new RelayCommand(RestoreDefault, CanRestoreDefault);
            // Populate orientation options (could also be done in LoadDetails)
            PopulateOrientationOptions();
            // Initial Load (delegated)
            _ = LoadInitialDataAsync();
        }


        // --- Initialization and Event Handling ---
        private async Task LoadInitialDataAsync()
        {
            await PresetManager.LoadPresetsAsync(); // Load presets via manager
            DeviceSelector.LoadDeviceList(); // Load devices via selector
            // Device change handler will load initial details if a device is selected
        }
        
        private void PopulateOrientationOptions()
        {
            AvailableOrientations.Clear();
            AvailableOrientations.Add(new DisplayOrientationItem { Name = "Landscape", Value = DisplayOrientation.Landscape });
            AvailableOrientations.Add(new DisplayOrientationItem { Name = "Portrait", Value = DisplayOrientation.Portrait });
            AvailableOrientations.Add(new DisplayOrientationItem { Name = "Landscape (flipped)", Value = DisplayOrientation.LandscapeFlipped });
            AvailableOrientations.Add(new DisplayOrientationItem { Name = "Portrait (flipped)", Value = DisplayOrientation.PortraitFlipped });
        }
        
        private void DeviceSelector_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceSelectorViewModel.SelectedDevice))
            {
                // When device changes, load details AND re-evaluate commands depending on device selection
                LoadDisplayDetailsForSelectedDevice(); // This already calls RaiseCanExecuteChanged on commands inside it or via SetProperty
                // Explicitly raise for ApplyPresetCommand as well, as its CanExecute depends on both device and preset
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RestoreDefaultCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Re-evaluate Restore command
                (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Re-evaluate Apply command
                (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();// Re-evaluate Save command
            }
        }

        private void PresetManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PresetManagerViewModel.SelectedPreset))
            {
                // When selected preset changes, re-evaluate commands depending on preset selection
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                // Note: DeletePresetCommand is inside PresetManagerViewModel, it handles its own CanExecute update.
            }
        }
        
        

        // --- Core Logic Methods (Simplified) ---

        private void LoadDisplayDetailsForSelectedDevice()
        {
            var device = DeviceSelector.SelectedDevice; // Get from selector

            // Clear/reset state if no device
            if (device?.DeviceName == null)
            {
                HeaderDisplayName = "No Device Selected";
                HeaderDisplayParameters = null;
                HeaderDeviceString = null;
                Resolutions.Clear();
                RefreshRates.Clear();
                SelectedResolution = null; // This will clear RefreshRates via setter chain
                Dpi = 100;
                MinDpi = 100;
                MaxDpi = 100;
                UpdateStatusColor();
                UpdateDpiRangeForResolution(null);
                return;
            }

            bool success = true;
            // Update Header
            HeaderDisplayName = FormatHeaderDisplayName(device); // Use helper
            HeaderDeviceString = device.DeviceString ?? "N/A";

            // Load Resolutions
            try
            {
                Resolutions.Clear();
                var modes = _infoService.GetSupportedModes(device.DeviceName);
                foreach (var mode in modes.DistinctBy(m => new { m.Width, m.Height }).OrderByDescending(m => m.Width)
                             .ThenByDescending(m => m.Height))
                {
                    Resolutions.Add(mode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading supported modes for {device.FriendlyName}: {ex.Message}");
                success = false;
            }

            // Get Current Mode/Scaling
            DisplayModeInfo? currentMode = null; DpiScalingInfo currentScaling = new();
            try
            {
                currentMode = _infoService.GetCurrentMode(device.DeviceName);
                currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                if (!currentScaling.IsInitialized)
                {
                    Console.WriteLine($"Warning: Scaling info not initialized for {device.FriendlyName}");
                    // Keep default scaling values (100)
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current mode/scaling for {device.FriendlyName}: {ex.Message}");
                success = false;
            }

            // Update Header Parameters (Actual)
            string currentResStr = (currentMode != null) ? $"{currentMode.Width}x{currentMode.Height}" : "N/A";
            string currentRateStr = (currentMode != null) ? $"{currentMode.RefreshRate}Hz" : "N/A";
            string currentDpiStr = $"{currentScaling.Current}% DPI";
            HeaderDisplayParameters = $"{currentResStr}, {currentRateStr}, {currentDpiStr}";

            // --- Set UI Selections ---
            // 1. Set Resolution (this will trigger UpdateDpiRangeForResolution via setter)
            var initialResolution = Resolutions.FirstOrDefault(m =>
                                        currentMode != null && m.Width == currentMode.Width &&
                                        m.Height == currentMode.Height)
                                    ?? Resolutions.FirstOrDefault();
            SelectedResolution = initialResolution; // This triggers UpdateRefreshRatesAndCommands
            OnPropertyChanged(nameof(SelectedResolution)); // Notify UI
            SelectedOrientation = AvailableOrientations.FirstOrDefault(o => o.Value == currentMode?.Orientation);
            
            // 2. Update Refresh Rates based on the just-set resolution
            UpdateRefreshRatesForSelectedResolution(currentMode?.RefreshRate); // Pass current rate
            // 3. Update DPI Range based on the just-set resolution
            UpdateDpiRangeForResolution(initialResolution); 

            // Set DPI Slider and Range (do this *after* SelectedResolution triggers updates)
            Dpi = currentScaling.Current;
            
            // Now set the refresh rate based on the potentially just-set resolution and actual current rate
            if (SelectedResolution == initialResolution && currentMode != null)
            {
                UpdateRefreshRatesForSelectedResolution(currentMode.RefreshRate);
            }
            // If SelectedResolution changed, its setter already called UpdateRefreshRates

            UpdateStatusColor(!success);
            // Ensure commands re-evaluate after loading all details
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RestoreDefaultCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Update refresh rates AND relevant command states
        private void UpdateRefreshRatesAndCommands()
        {
            UpdateRefreshRatesForSelectedResolution();
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Save preset depends on valid Res/Rate
        }

        private void UpdateRefreshRatesForSelectedResolution(int? targetRefreshRate = null)
        {
            var device = DeviceSelector.SelectedDevice;
            if (SelectedResolution == null || device?.DeviceName == null)
            {
                RefreshRates.Clear();
                SelectedRefreshRate = 0;
                return;
            }

            // ... (Logic to populate RefreshRates based on SelectedResolution and device) ...
            RefreshRates.Clear();
            var supportedRates = _infoService.GetSupportedModes(device.DeviceName)
                .Where(m => m.Width == SelectedResolution.Width && m.Height == SelectedResolution.Height)
                .Select(m => m.RefreshRate).Distinct().OrderBy(r => r);
            foreach (var rate in supportedRates) RefreshRates.Add(rate);

            // ... (Set SelectedRefreshRate based on targetRefreshRate or default) ...
            SelectedRefreshRate = RefreshRates.Contains(targetRefreshRate ?? _selectedRefreshRate)
                ? (targetRefreshRate ?? _selectedRefreshRate)
                : RefreshRates.FirstOrDefault();
        }

        // --- NEW: Method to update DPI range ---
        private void UpdateDpiRangeForResolution(DisplayModeInfo? newResolution)
        {
            uint newMinDpi = 100; // Assuming minimum is always 100
            uint newMaxDpi;

            if (newResolution == null || newResolution.Height <= 0)
            {
                newMaxDpi = 200; // Default max
            }
            else
            {
                newMaxDpi = EstimateMaxDpiForHeight(newResolution.Height);
            }

            // --- Refined Update Logic ---
            // Store the current value *before* changing the range
            uint currentDpiValue = _dpi;

            // 1. Update the range properties. SetProperty will notify the UI.
            //    It's important that these notifications happen *before* we potentially set Dpi again.
            MinDpi = newMinDpi;
            MaxDpi = newMaxDpi;

            // 2. Calculate the clamped value based on the *original* value and the *new* range.
            uint clampedDpi = Math.Clamp(currentDpiValue, newMinDpi, newMaxDpi);

            // 3. Set the Dpi property using the calculated clamped value.
            //    This ensures that even if the original value was within the new range,
            //    a PropertyChanged notification for Dpi is sent *after* Min/Max have changed,
            //    which might help the Slider sync up.
            Dpi = clampedDpi;
            // --- End Refined Update Logic ---


            System.Diagnostics.Debug.WriteLine($"DPI Range updated for H={newResolution?.Height}: Min={MinDpi}, Max={MaxDpi}, Current DPI set to/remains {Dpi}");
        }
        
        // --- Command Implementations (Delegating) ---
        
        private bool CanApplySettings() => DeviceSelector.SelectedDevice != null && SelectedResolution != null &&
                                           SelectedRefreshRate > 0;
        private async Task ApplySettingsAsync()
        {
            // Use SelectedOrientation.Value when calling applicator
            if (!CanApplySettings()) return;
            var device = DeviceSelector.SelectedDevice!;
            var resolution = SelectedResolution!;
            var orientation = SelectedOrientation!.Value; // Get enum value

            Console.WriteLine($"MainVM: Applying settings to {device.FriendlyName}: {resolution.Width}x{resolution.Height}, {SelectedRefreshRate}Hz, {Dpi}%, {orientation}");

            var request = new DisplayConfigRequest
            {
                DeviceName = device.DeviceName!,
                Width = resolution.Width,
                Height = resolution.Height,
                RefreshRate = SelectedRefreshRate,
                Orientation = orientation // Set the orientation in the request
                // Position, Primary etc. could also be set here if needed
            };
            
            // Applicator needs modification
            bool success = await _applicator.ApplySettingsAsync(device,request,Dpi); 

            if (success) {
                Console.WriteLine("MainVM: ApplySettings success. Reloading details...");
                LoadDisplayDetailsForSelectedDevice(); // Reload to reflect actual state
            } else {
                Console.WriteLine("MainVM: ApplySettings failed.");
                UpdateStatusColor(true);
            }
        }

        private bool CanSavePreset() => DeviceSelector.SelectedDevice != null && SelectedResolution != null &&
                                        SelectedRefreshRate > 0;

        private async Task SavePresetAsync()
        {
            if (!CanSavePreset()) return;

            var presetValues = new DisplayPreset
            {
                Width = SelectedResolution!.Width, Height = SelectedResolution.Height,
                RefreshRate = SelectedRefreshRate, Dpi = Dpi
            };

            if (PresetManager.PresetExists(presetValues))
            {
                Console.WriteLine("Preset with these settings already exists.");
                /* Inform user */
                return;
            }

            // TODO: Get name from user
            string presetName = $"Preset {PresetManager.Presets.Count + 1}";
            presetValues.Name = presetName;

            bool success = await PresetManager.AddAndSavePresetAsync(presetValues);
            if (success)
            {
                // Optional: Select the new preset in the manager's list
                PresetManager.SelectedPreset = presetValues;
            }
            else
            {
                // TODO: Inform user save failed
            }
        }

        private bool CanApplyPreset() => PresetManager.SelectedPreset != null && DeviceSelector.SelectedDevice != null;

        private async Task ApplyPresetAsync()
        {
            if (!CanApplyPreset()) return;
            var device = DeviceSelector.SelectedDevice!;
            var preset = PresetManager.SelectedPreset!;

            bool success = await _applicator.ApplyPresetAsync(device, preset);

            if (success)
            {
                Console.WriteLine("MainVM: ApplyPreset success. Reloading details...");
                LoadDisplayDetailsForSelectedDevice(); // Reload to reflect actual state
            }
            else
            {
                Console.WriteLine("MainVM: ApplyPreset failed (or incompatible).");
                UpdateStatusColor(true);
                // TODO: Notify User
            }
        }

        private bool CanRestoreDefault() => DeviceSelector.SelectedDevice != null;

        private void RestoreDefault()
        {
            Console.WriteLine("MainVM: RestoreDefault command executed.");
            LoadDisplayDetailsForSelectedDevice(); // Just reload current settings
        }
        
        private void UpdateStatusColor(bool error = false)
        {
            StatusColor = Brushes.LimeGreen; // 正常
        }


        // --- Formatting Helpers (Move to Utils or Display) ---
        private string FormatHeaderDisplayName(DisplayDeviceInfo deviceInfo)
        {
            /* ... */
            return $"[{deviceInfo.SourceId}] {deviceInfo.FriendlyName ?? "Unknown"} ({FormatOutputTechnology(deviceInfo.OutputTechnology)})";
        }

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

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper to reduce boilerplate
        protected bool SetProperty<T>(ref T field, T value, Action? onChanged = null,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke();
            return true;
        }
        
        // Helper function to estimate MaxDPI based on height ---
        private static uint EstimateMaxDpiForHeight(int height)
        {
            if (height <= 0) return 100; // Default or minimum

            // Formula derived from data: MaxDPI ≈ RoundToNearestMultiple( Height * 0.1615 + 1, 25 )
            double estimatedValue = (height * 0.1615) + 1.0;
            uint roundedMaxDpi = (uint)(Math.Round(estimatedValue / 25.0) * 25.0);

            // Ensure minimum is at least 100
            return Math.Max(100, roundedMaxDpi);
        }
        
        // Helper class for ComboBox display
        public class DisplayOrientationItem
        {
            public string Name { get; set; } = "";
            public DisplayOrientation Value { get; set; }
            // Optional: Override Equals/GetHashCode if needed for SelectedItem comparison
            public override bool Equals(object? obj) => obj is DisplayOrientationItem item && Value == item.Value;
            public override int GetHashCode() => Value.GetHashCode();
        }
        
    }
}