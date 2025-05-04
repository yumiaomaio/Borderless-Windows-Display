using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Services.Presets;
using BorderlessWindowApp.Views;


namespace BorderlessWindowApp.ViewModels.Display
{
    // Helper class for ComboBox display (keep or move to Models)
    public class
        DisplayOrientationItem : INotifyPropertyChanged // Implement INPC if Name can change? Usually not needed.
    {
        public string Name { get; set; } = "";
        public DisplayOrientation Value { get; set; }
        public override bool Equals(object? obj) => obj is DisplayOrientationItem item && Value == item.Value;
        public override int GetHashCode() => Value.GetHashCode();

        // Basic INPC implementation if needed
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DisplaySettingsViewModel : INotifyPropertyChanged, IDisposable // Added IDisposable for cleanup
    {
        // --- Injected Dependencies ---
        private readonly IDisplayInfoService _infoService;
        private readonly IDisplayScaleService _scaleService;
        private readonly IDisplaySettingsApplicator _applicator;
        private readonly ILogger<DisplaySettingsViewModel> _logger; // Use real logger
        public DeviceSelectorViewModel DeviceSelector { get; } // Keep public for binding
        public PresetManagerViewModel PresetManager { get; } // Keep public for binding

        // --- State specific to the selected device's *details* ---
        public ObservableCollection<DisplayModeInfo> Resolutions { get; } = new();
        public ObservableCollection<int> RefreshRates { get; } = new();
        public ObservableCollection<DisplayOrientationItem> AvailableOrientations { get; } = new();

        private DisplayModeInfo? _selectedResolution;

        public DisplayModeInfo? SelectedResolution
        {
            get => _selectedResolution;
            set { SetProperty(ref _selectedResolution, value, OnResolutionChanged); } // Use specific handler
        }

        private int _selectedRefreshRate;

        public int SelectedRefreshRate
        {
            get => _selectedRefreshRate;
            set { SetProperty(ref _selectedRefreshRate, value, UpdateCommandsForCurrentSettings); }
        }

        private DisplayOrientationItem? _selectedOrientation;

        public DisplayOrientationItem? SelectedOrientation
        {
            get => _selectedOrientation;
            set { SetProperty(ref _selectedOrientation, value, UpdateCommandsForCurrentSettings); }
        }

        private uint _dpi = 100;

        public uint Dpi
        {
            get => _dpi;
            set
            {
                uint clampedValue = Math.Clamp(value, MinDpi, MaxDpi);
                SetProperty(ref _dpi, clampedValue, UpdateCommandsForCurrentSettings);
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

        // Stores the state loaded or last successfully applied
        private DisplayModeInfo? _lastKnownGoodResolution;
        private int _lastKnownGoodRefreshRate;
        private DisplayOrientationItem? _lastKnownGoodOrientation;
        private uint _lastKnownGoodDpi;
        private bool _isConfirmationRequired;

        public bool IsConfirmationRequired
        {
            get => _isConfirmationRequired;
            set => SetProperty(ref _isConfirmationRequired, value);
        }
        // --- End NEW State ---

        // --- Commands (Coordination) ---
        public ICommand ApplyCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand ApplyPresetCommand { get; }
        public ICommand RevertChangesCommand { get; }


        // --- Constructor (Inject Dependencies) ---
        public DisplaySettingsViewModel(
            DeviceSelectorViewModel deviceSelector,
            PresetManagerViewModel presetManager,
            IDisplayInfoService infoService,
            IDisplayScaleService scaleService,
            IDisplaySettingsApplicator applicator,
            ILogger<DisplaySettingsViewModel> logger) // Inject logger
        {
            // Assign injected dependencies
            DeviceSelector = deviceSelector ?? throw new ArgumentNullException(nameof(deviceSelector));
            PresetManager = presetManager ?? throw new ArgumentNullException(nameof(presetManager));
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
            _applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Assign logger

            _logger.LogInformation("DisplaySettingsViewModel initializing...");

            // Subscribe to device selection changes
            DeviceSelector.PropertyChanged += DeviceSelector_PropertyChanged;
            PresetManager.PropertyChanged += PresetManager_PropertyChanged;

            // Initialize Commands
            ApplyCommand = new RelayCommand(async () => await ApplySettingsAsync(), CanApplySettings);
            SavePresetCommand = new RelayCommand(async () => await SavePresetAsync(), CanSavePreset);
            ApplyPresetCommand = new RelayCommand(async () => await ApplyPresetAsync(), CanApplyPreset);
            RevertChangesCommand = new RelayCommand(RevertChanges, CanRevertChanges);

            // Populate static options
            PopulateOrientationOptions();

            // Initial Load (delegated)
            _ = LoadInitialDataAsync();
            _logger.LogInformation("DisplaySettingsViewModel initialized.");
        }


        // --- Initialization and Event Handling ---
        private async Task LoadInitialDataAsync()
        {
            _logger.LogInformation("Loading initial data...");
            // Use try-catch for robustness
            try
            {
                await PresetManager.LoadPresetsAsync(); // Load presets via manager
                DeviceSelector.LoadDeviceList(); // Load devices via selector
                // Device change handler will load initial details if a device is selected
                _logger.LogInformation("Initial data loading complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial data loading.");
                // TODO: Show error to user
                UpdateStatusColor(true); // Indicate error
            }
        }

        private void PopulateOrientationOptions()
        {
            AvailableOrientations.Clear();
            AvailableOrientations.Add(new DisplayOrientationItem
                { Name = "Landscape", Value = DisplayOrientation.Landscape });
            AvailableOrientations.Add(new DisplayOrientationItem
                { Name = "Portrait", Value = DisplayOrientation.Portrait });
            AvailableOrientations.Add(new DisplayOrientationItem
                { Name = "Landscape (flipped)", Value = DisplayOrientation.LandscapeFlipped });
            AvailableOrientations.Add(new DisplayOrientationItem
                { Name = "Portrait (flipped)", Value = DisplayOrientation.PortraitFlipped });
        }

        // Handles changes from DeviceSelectorViewModel
        private void DeviceSelector_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceSelectorViewModel.SelectedDevice))
            {
                _logger.LogDebug("SelectedDevice changed, reloading details and updating commands.");
                LoadDisplayDetailsForSelectedDevice(); // Reload details for new device
                // Explicitly update commands that depend on SelectedDevice
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RevertChangesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                // Apply/Save commands are implicitly updated by LoadDisplayDetails via SetProperty/OnResolutionChanged
            }
        }

        // Handles changes from PresetManagerViewModel
        private void PresetManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PresetManagerViewModel.SelectedPreset))
            {
                _logger.LogDebug("SelectedPreset changed, updating ApplyPresetCommand state.");
                // Update commands that depend on SelectedPreset
                (ApplyPresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // --- Core Logic Methods ---

        private void LoadDisplayDetailsForSelectedDevice()
        {
            var device = DeviceSelector.SelectedDevice;
            _logger.LogDebug("Loading details for device: {DeviceName}", device?.FriendlyName ?? "None");

            if (device?.DeviceName == null)
            {
                // Reset state when no device is selected
                _logger.LogDebug("No device selected, resetting details.");
                HeaderDisplayName = "No Device Selected";
                HeaderDisplayParameters = null;
                HeaderDeviceString = null;
                Resolutions.Clear();
                RefreshRates.Clear();
                // Setting SelectedResolution to null triggers OnResolutionChanged which clears rates and updates commands
                SelectedResolution = null;
                // Reset DPI separately
                MinDpi = 100;
                MaxDpi = 100;
                Dpi = 100;
                SelectedOrientation = null;
                UpdateStatusColor();
                return;
            }

            bool success = true;
            HeaderDisplayName = FormatHeaderDisplayName(device);
            HeaderDeviceString = device.DeviceString ?? "N/A";

            // --- Load Data (Wrap service calls in try-catch) ---
            DisplayModeInfo? currentMode = null;
            DpiScalingInfo currentScaling = new() { Minimum = 100, Maximum = 200, Current = 100 }; // Safe defaults
            List<DisplayModeInfo> modes = new();

            try
            {
                modes = _infoService.GetSupportedModes(device.DeviceName)?.ToList() ?? new List<DisplayModeInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supported modes for {Device}", device.FriendlyName);
                success = false;
            }

            try
            {
                currentMode = _infoService.GetCurrentMode(device.DeviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current mode for {Device}", device.FriendlyName);
                success = false;
            }

            try
            {
                currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                if (!currentScaling.IsInitialized)
                    _logger.LogWarning("Scaling info not initialized for {Device}", device.FriendlyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scaling info for {Device}", device.FriendlyName);
                success = false;
            }

            // Update Header Parameters (Based on loaded actual values)
            string currentResStr = (currentMode != null) ? $"{currentMode.Width}x{currentMode.Height}" : "N/A";
            string currentRateStr = (currentMode != null) ? $"{currentMode.RefreshRate}Hz" : "N/A";
            string currentDpiStr = $"{currentScaling.Current}% DPI";
            HeaderDisplayParameters = $"{currentResStr}, {currentRateStr}, {currentDpiStr}";

            // --- Set UI Selections ---
            // 1. Populate Resolutions List (use loaded modes)
            Resolutions.Clear();
            foreach (var mode in modes.DistinctBy(m => new { m.Width, m.Height }).OrderByDescending(m => m.Width)
                         .ThenByDescending(m => m.Height))
            {
                Resolutions.Add(mode);
            }

            // 2. Set Selected Resolution (this triggers OnResolutionChanged)
            var initialResolution =
                Resolutions.FirstOrDefault(m =>
                    currentMode != null && m.Width == currentMode.Width && m.Height == currentMode.Height) ??
                Resolutions.FirstOrDefault();
            SelectedResolution =
                initialResolution; // This triggers refresh rate and DPI range updates via OnResolutionChanged

            // 3. Set Selected Orientation based on actual current orientation
            SelectedOrientation =
                AvailableOrientations.FirstOrDefault(o => currentMode != null && o.Value == currentMode.Orientation);

            // 4. Set Dpi value (let setter clamp based on range set by resolution change)
            Dpi = currentScaling.Current;

            // 5. Set Refresh Rate (must happen *after* SelectedResolution is set and RefreshRates list is populated)
            if (currentMode != null && SelectedResolution == initialResolution)
            {
                // If resolution matched current, try to select current rate
                UpdateRefreshRatesForSelectedResolution(currentMode.RefreshRate);
            }
            // If resolution didn't match current (or current was null),
            // OnResolutionChanged->UpdateRefreshRatesAndCommands->UpdateRefreshRatesForSelectedResolution
            // already set the default refresh rate.

            // Store the loaded state as "last known good" ---
            // Do this AFTER setting the properties based on loaded data
            _lastKnownGoodResolution = SelectedResolution;
            _lastKnownGoodRefreshRate = SelectedRefreshRate;
            _lastKnownGoodOrientation = SelectedOrientation;
            _lastKnownGoodDpi = Dpi;
            _logger.LogDebug("Stored last known good state.");
            // --- End NEW ---

            UpdateStatusColor(!success);
            // Commands are updated via SetProperty/OnResolutionChanged, no explicit RaiseCanExecuteChanged needed here anymore.
        }

        // Called when SelectedResolution changes
        private void OnResolutionChanged()
        {
            _logger.LogDebug("SelectedResolution changed, updating RefreshRates, DPI Range, and Commands.");
            UpdateRefreshRatesAndCommands(); // Update refresh rates list and dependent commands
            UpdateDpiRangeForResolution(SelectedResolution); // Update DPI range based on new resolution
        }

        // Updates refresh rate list AND dependent command states
        private void UpdateRefreshRatesAndCommands()
        {
            UpdateRefreshRatesForSelectedResolution(); // Update list and selection
            UpdateCommandsForCurrentSettings(); // Update command states based on current selections
        }

        // Updates refresh rate list and selects appropriate rate
        private void UpdateRefreshRatesForSelectedResolution(int? targetRefreshRate = null)
        {
            // TODO: Implementation as before (uses DeviceSelector.SelectedDevice, SelectedResolution)
            // Populates RefreshRates collection and sets SelectedRefreshRate
            var device = DeviceSelector.SelectedDevice;
            if (SelectedResolution == null || device?.DeviceName == null)
            {
                RefreshRates.Clear();
                SelectedRefreshRate = 0;
                return;
            }

            try
            {
                RefreshRates.Clear();
                var supportedRates = _infoService.GetSupportedModes(device.DeviceName)
                    .Where(m => m.Width == SelectedResolution.Width && m.Height == SelectedResolution.Height)
                    .Select(m => m.RefreshRate).Distinct().OrderBy(r => r);
                foreach (var rate in supportedRates) RefreshRates.Add(rate);
                SelectedRefreshRate = RefreshRates.Contains(targetRefreshRate ?? _selectedRefreshRate)
                    ? (targetRefreshRate ?? _selectedRefreshRate)
                    : RefreshRates.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh rates for {Res}", SelectedResolution);
                RefreshRates.Clear();
                SelectedRefreshRate = 0;
            }
        }

        // Updates DPI range based on resolution
        private void UpdateDpiRangeForResolution(DisplayModeInfo? newResolution)
        {
            // TODO: Implementation as before (estimates MaxDpi, sets MinDpi/MaxDpi, clamps Dpi)
            uint newMinDpi = 100;
            uint newMaxDpi;
            if (newResolution == null || newResolution.Height <= 0)
            {
                newMaxDpi = 200;
            }
            else
            {
                newMaxDpi = EstimateMaxDpiForHeight(newResolution.Height);
            }

            uint currentDpiValue = _dpi;
            MinDpi = newMinDpi;
            MaxDpi = newMaxDpi;
            Dpi = Math.Clamp(currentDpiValue, newMinDpi, newMaxDpi); // Let Dpi setter handle clamp and notify
        }

        // Updates command states based on current selections
        private void UpdateCommandsForCurrentSettings()
        {
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SavePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        // --- Command Implementations (Delegating) ---

        private bool CanApplySettings() => DeviceSelector.SelectedDevice != null && SelectedResolution != null &&
                                           SelectedRefreshRate > 0 && SelectedOrientation != null;

        private async Task ApplySettingsAsync()
        {
            if (!CanApplySettings()) return;
            var device = DeviceSelector.SelectedDevice!;
            var resolution = SelectedResolution!;
            var orientation = SelectedOrientation!.Value;
            int refreshRate = SelectedRefreshRate;
            uint dpiValue = Dpi;
            _logger.LogInformation("Executing ApplySettings command...");

            bool needConfirmation = IsConfirmationRequired;
            if (needConfirmation)
            {
                _logger.LogDebug("Storing state before apply...");
            }

            // Create request object
            var request = new DisplayConfigRequest
            {
                /* ... populate W, H, Rate, Orientation ... */
                DeviceName = device.DeviceName!, Width = resolution.Width, Height = resolution.Height,
                RefreshRate = SelectedRefreshRate, Orientation = orientation, BitDepth = 32
            };

            // Call applicator
            bool success = await _applicator.ApplySettingsAsync(device, request, Dpi);

            // Handle result
            if (success)
            {
                _logger.LogInformation("Applicator applied settings successfully.");

                if (needConfirmation)
                {
                    _logger.LogInformation("Confirmation required. Showing dialog...");
                    // --- Confirmation Logic ---
                    bool userConfirmed =
                        await ShowConfirmationDialogAsync("Keep new display settings?", TimeSpan.FromSeconds(5));

                    if (userConfirmed)
                    {
                        _logger.LogInformation("User confirmed settings.");
                        // New settings are kept. Reload details to update header
                        // and set the new state as the "last known good" state.
                        LoadDisplayDetailsForSelectedDevice();
                    }
                    else
                    {
                        _logger.LogWarning("User did not confirm or timeout expired. Reverting settings...");
                        // --- Revert Logic ---
                        // Create a request object for the state BEFORE this apply attempt
                        // (which is stored in _lastKnownGood... fields)
                        var revertRequest = new DisplayConfigRequest
                        {
                            DeviceName = device.DeviceName!,
                            Width = _lastKnownGoodResolution?.Width ?? resolution.Width, // Use fallback just in case
                            Height = _lastKnownGoodResolution?.Height ?? resolution.Height,
                            RefreshRate = _lastKnownGoodRefreshRate,
                            Orientation = _lastKnownGoodOrientation?.Value
                            // BitDepth etc.
                        };
                        uint revertDpi = _lastKnownGoodDpi;

                        // Call applicator again to revert
                        bool revertSuccess = await _applicator.ApplySettingsAsync(device, revertRequest, revertDpi);
                        if (revertSuccess)
                        {
                            _logger.LogInformation("Revert successful. Reloading details...");
                        }
                        else
                        {
                            _logger.LogError("REVERT FAILED! Display may be in an unexpected state.");
                            // TODO: Notify user of revert failure! Critical state.
                        }

                        // Always reload details after revert attempt to show actual state
                        LoadDisplayDetailsForSelectedDevice();
                        // --- End Revert Logic ---
                    }
                    // --- End Confirmation Logic ---
                }
                else // No confirmation needed, apply succeeded
                {
                    _logger.LogInformation("Confirmation not required. Reloading details...");
                    // Apply was successful, reload details to update header
                    // and set the applied state as the "last known good" state.
                    LoadDisplayDetailsForSelectedDevice();
                }
            }
            else // Initial apply failed
            {
                _logger.LogWarning("Apply settings failed via applicator.");
                UpdateStatusColor(true);
                // TODO: Notify User of apply failure
                // Do NOT update _lastKnownGood state
            }
        }


        private bool CanSavePreset() => DeviceSelector.SelectedDevice != null && SelectedResolution != null &&
                                        SelectedRefreshRate > 0 && SelectedOrientation != null;

        private async Task SavePresetAsync()
        {
            if (!CanSavePreset()) return;
            _logger.LogInformation("Executing SavePreset command...");
            // Create preset object
            var presetValues = new DisplayPreset
            {
                /* ... populate W, H, Rate, DPI, Orientation ... */ Width = SelectedResolution!.Width,
                Height = SelectedResolution.Height, RefreshRate = SelectedRefreshRate, Dpi = Dpi,
                Orientation = SelectedOrientation!.Value
            };
            // Check existence via manager
            if (PresetManager.PresetExists(presetValues))
            {
                _logger.LogWarning("Preset already exists."); /* TODO: Notify User */
                return;
            }

            // Get name (TODO: Implement user input dialog)
            string presetName = $"Preset {PresetManager.Presets.Count + 1}"; // Placeholder
            presetValues.Name = presetName;
            // Add via manager
            bool success = await PresetManager.AddAndSavePresetAsync(presetValues);
            if (success)
            {
                _logger.LogInformation("Preset '{PresetName}' saved.", presetName);
                PresetManager.SelectedPreset = presetValues;
            }
            else
            {
                _logger.LogWarning("Failed to save preset '{PresetName}'.", presetName); /* TODO: Notify User */
            }
        }


        private bool CanApplyPreset() => PresetManager.SelectedPreset != null && DeviceSelector.SelectedDevice != null;

        private async Task ApplyPresetAsync()
        {
            // 1. Check if command can execute
            if (!CanApplyPreset()) return;

            // 2. Get the required parameters safely
            var device = DeviceSelector.SelectedDevice;
            var preset = PresetManager.SelectedPreset;
            if (device == null || preset == null || device.DeviceName == null)
            {
                _logger.LogError("ApplyPresetAsync: Pre-conditions not met (Device, Preset, or DeviceName is null).");
                return;
            }

            bool needConfirmation = IsConfirmationRequired;
            _logger.LogInformation(
                "Executing ApplyPreset command for '{PresetName}'. Confirmation required: {NeedConfirmation}",
                preset.Name, needConfirmation);

            // 3. Call the applicator to apply the PRESET settings
            bool success = await _applicator.ApplyPresetAsync(device, preset); // Applicator handles compatibility

            if (success)
            {
                _logger.LogInformation("Applicator applied preset '{PresetName}' successfully.", preset.Name);

                // 4. Handle Confirmation if needed
                if (needConfirmation)
                {
                    _logger.LogInformation("Confirmation required. Showing dialog...");
                    bool userConfirmed =
                        await ShowConfirmationDialogAsync($"Keep settings from preset '{preset.Name}'?",
                            TimeSpan.FromSeconds(5));

                    if (userConfirmed)
                    {
                        _logger.LogInformation("User confirmed preset settings.");
                        // Settings are kept. Reload details to update UI header and set the new "last known good" state.
                        LoadDisplayDetailsForSelectedDevice();
                    }
                    else
                    {
                        _logger.LogWarning("User did not confirm preset or timeout expired. Reverting settings...");

                        // --- Revert Logic ---
                        // Create a request object to revert to the state BEFORE this preset apply attempt.
                        // This state is stored in the _lastKnownGood... fields.

                        // Check if we have valid "last known good" state to revert to
                        if (_lastKnownGoodResolution != null && _lastKnownGoodOrientation != null)
                        {
                            var revertRequest = new DisplayConfigRequest
                            {
                                DeviceName = device.DeviceName, // Use the same device name
                                Width = _lastKnownGoodResolution.Width,
                                Height = _lastKnownGoodResolution.Height,
                                RefreshRate = _lastKnownGoodRefreshRate,
                                Orientation = _lastKnownGoodOrientation.Value, // Use the stored orientation enum value
                                // Ensure other necessary fields like BitDepth are set if ApplySettingsAsync requires them
                            };
                            uint revertDpi = _lastKnownGoodDpi;

                            _logger.LogDebug("Attempting to revert to: W={W}, H={H}, R={R}, O={O}, DPI={DPI}",
                                revertRequest.Width, revertRequest.Height, revertRequest.RefreshRate,
                                revertRequest.Orientation, revertDpi);

                            // Call applicator again to revert using the last known good settings
                            bool revertSuccess = await _applicator.ApplySettingsAsync(device, revertRequest, revertDpi);

                            if (revertSuccess)
                            {
                                _logger.LogInformation("Revert successful.");
                            }
                            else
                            {
                                _logger.LogError(
                                    "REVERT FAILED after preset apply! Display may be in an unexpected state.");
                                // TODO: Notify user of critical revert failure!
                                UpdateStatusColor(true); // Indicate error state
                            }
                        }
                        else
                        {
                            _logger.LogError("Cannot revert: Last known good state is missing.");
                            // TODO: Notify user? This indicates a potential logic error earlier.
                            UpdateStatusColor(true);
                        }

                        // Always reload details after revert attempt (successful or not) to show actual current state
                        LoadDisplayDetailsForSelectedDevice();
                        // --- End Revert Logic ---
                    }
                }
                else // No confirmation needed, apply succeeded
                {
                    _logger.LogInformation("Confirmation not required. Reloading details...");
                    // Reload details to update UI header and set the applied state as the new "last known good" state.
                    LoadDisplayDetailsForSelectedDevice();
                }
            }
            else // Initial preset apply failed
            {
                _logger.LogWarning("Apply preset '{PresetName}' failed via applicator.", preset.Name);
                UpdateStatusColor(true);
                // TODO: Notify User
            }
        }


        private bool CanRevertChanges()
        {
            // Enable revert if current UI state differs from last known good state
            // (Requires DisplayModeInfo and DisplayOrientationItem to have proper Equals overrides)
            return DeviceSelector.SelectedDevice != null &&
                   (!Equals(SelectedResolution, _lastKnownGoodResolution) ||
                    SelectedRefreshRate != _lastKnownGoodRefreshRate ||
                    !Equals(SelectedOrientation, _lastKnownGoodOrientation) ||
                    Dpi != _lastKnownGoodDpi);
        }

        private void RevertChanges()
        {
            _logger.LogInformation("Executing RevertChanges command.");
            if (DeviceSelector.SelectedDevice == null) return; // Should be covered by CanExecute

            // Restore UI properties from the stored "last known good" state
            // Important: Set quietly first if setters trigger complex logic, then notify
            // Or rely on SetProperty to handle recursion guards if present

            // Find the matching objects for reference types
            var resToSelect = Resolutions.FirstOrDefault(r => Equals(r, _lastKnownGoodResolution));
            // Ensure we fallback gracefully if the exact mode object isn't in the current list (unlikely but possible)
            if (resToSelect == null && _lastKnownGoodResolution != null)
            {
                // Try finding by value match if object reference differs
                resToSelect = Resolutions.FirstOrDefault(r =>
                    r.Width == _lastKnownGoodResolution.Width && r.Height == _lastKnownGoodResolution.Height);
            }

            var orientToSelect = AvailableOrientations.FirstOrDefault(o => Equals(o, _lastKnownGoodOrientation));

            // Update properties - SetProperty will trigger necessary updates (like refresh rates)
            SelectedResolution = resToSelect; // This triggers OnResolutionChanged -> UpdateRefreshRates/DPI Range
            SelectedOrientation = orientToSelect;
            Dpi = _lastKnownGoodDpi;

            // SelectedRefreshRate will be updated by the UpdateRefreshRates call triggered by SelectedResolution setter
            // But we should ensure the correct *specific* rate is selected if possible
            // Do this *after* SelectedResolution setter has run its course (e.g., Dispatcher or just set it again)
            // For simplicity now, we rely on UpdateRefreshRates setting a default or finding the rate.
            // A more robust way is needed if the exact _lastKnownGoodRefreshRate MUST be restored even if list changed.
            // Let's ensure the refresh rate setter is called *after* the list is potentially repopulated.
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                // Ensure list update finishes
                if (RefreshRates.Contains(_lastKnownGoodRefreshRate))
                {
                    SelectedRefreshRate = _lastKnownGoodRefreshRate;
                }
                else
                {
                    // If the specific rate isn't available for the reverted resolution, log it.
                    // UpdateRefreshRates already picked a default.
                    _logger.LogWarning(
                        "Last known good refresh rate {Rate} not available for reverted resolution {Res}. Default selected.",
                        _lastKnownGoodRefreshRate, SelectedResolution);
                }
            }, System.Windows.Threading.DispatcherPriority.Background); // Lower priority


            _logger.LogInformation("UI reverted to last known good state.");
            (RevertChangesCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Should now be disabled
        }

        private async Task<bool> ShowConfirmationDialogAsync(string message, TimeSpan timeout)
        {
            _logger.LogDebug("Creating and showing confirmation dialog.");
            // In a real app, consider injecting an IDialogService to handle this
            var dialog = new ConfirmationDialog(message, timeout);

            // Show the dialog non-modally and await its result
            dialog.Show();
            bool result = await dialog.GetResultAsync(); // Await the TaskCompletionSource

            _logger.LogInformation("Confirmation dialog returned: {Result} (True=Keep, False=Revert/Timeout)", result);
            return result;
        }

        private void UpdateStatusColor(bool error = false)
        {
            // TODO: Implementation as before (check SelectedDevice.IsAvailable, error flag)
            var device = DeviceSelector.SelectedDevice;
            if (error) StatusColor = Brushes.Red;
            else if (device == null) StatusColor = Brushes.Gray;
            else if (!device.IsAvailable) StatusColor = Brushes.Orange; // Assuming IsAvailable exists
            else StatusColor = Brushes.LimeGreen;
        }


        // --- Formatting Helpers ---
        private string FormatHeaderDisplayName(DisplayDeviceInfo deviceInfo)
        {
            return
                $"[{deviceInfo.SourceId}] {deviceInfo.FriendlyName ?? "Unknown"} ({FormatOutputTechnology(deviceInfo.OutputTechnology)})";
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

        private static uint EstimateMaxDpiForHeight(int height)
        {
            if (height <= 0) return 100; // Default or minimum

            // Formula derived from data: MaxDPI ≈ RoundToNearestMultiple( Height * 0.1615 + 1, 25 )
            double estimatedValue = (height * 0.1615) + 1.0;
            uint roundedMaxDpi = (uint)(Math.Round(estimatedValue / 25.0) * 25.0);

            // Ensure minimum is at least 100
            return Math.Max(100, roundedMaxDpi);
        }


        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // No command updates needed here - handled by SetProperty/Event Handlers/Specific methods
        }

        protected bool SetProperty<T>(ref T field, T value, Action? onChanged = null,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke(); // Execute associated action
            return true;
        }

        // --- IDisposable (Optional but good practice for event unsubscription) ---
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events to prevent memory leaks
                    DeviceSelector.PropertyChanged -= DeviceSelector_PropertyChanged;
                    PresetManager.PropertyChanged -= PresetManager_PropertyChanged;
                    _logger.LogInformation("DisplaySettingsViewModel disposed.");
                }

                _disposed = true;
            }
        }
        // Add finalizer if dealing with unmanaged resources, though unlikely here
        // ~DisplaySettingsViewModel() { Dispose(false); }
    } // End Class
} // End Namespace