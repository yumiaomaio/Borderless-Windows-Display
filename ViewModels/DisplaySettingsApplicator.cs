
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;


namespace BorderlessWindowApp.ViewModels
{
    public class DisplaySettingsApplicator : IDisplaySettingsApplicator
    {
        private readonly IDisplayConfigService _configService;
        private readonly IDisplayScaleService _scaleService;
        private readonly IDisplayInfoService _infoService; // Needed for compatibility checks

        public DisplaySettingsApplicator(
            IDisplayConfigService configService,
            IDisplayScaleService scaleService,
            IDisplayInfoService infoService) // Inject InfoService
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService)); // Assign InfoService
        }

        public async Task<bool> ApplySettingsAsync(DisplayDeviceInfo device, DisplayModeInfo resolution, int refreshRate, uint dpi)
        {
            if (device?.DeviceName == null || resolution == null || refreshRate <= 0)
                return false;

            Console.WriteLine($"Applicator: Applying settings to {device.FriendlyName}: {resolution.Width}x{resolution.Height}, {refreshRate}Hz, {dpi}%");

            // Apply Config (Consider making service async if possible)
            var configRequest = new DisplayConfigRequest
            {
                DeviceName = device.DeviceName,
                Width = resolution.Width,
                Height = resolution.Height,
                RefreshRate = refreshRate
            };
            bool configApplied = _configService.ApplyDisplayConfiguration(configRequest);

            // Apply Scaling
            bool scaleApplied = false;
            try
            {
                // Check if scaling needs to be applied
                var currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                 if (currentScaling.IsInitialized && currentScaling.Current != dpi)
                 {
                     scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, dpi);
                 } else if (!currentScaling.IsInitialized) {
                     Console.WriteLine("Warning (Applicator): Could not get current scaling info. Attempting to set DPI anyway.");
                     scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, dpi);
                 } else {
                     scaleApplied = true; // DPI was already correct
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Applicator: Error applying scaling: {ex.Message}");
                // Do not count scaleApplied as true if exception occurred
                scaleApplied = false;
            }

            // Consider success only if both operations were successful OR didn't need to change
            // For simplicity here, we check if the intended operations succeeded.
            // A more robust check might re-query the actual state.
            bool overallSuccess = configApplied && scaleApplied;
            Console.WriteLine($"Applicator: ApplySettings result for {device.FriendlyName} - ConfigApplied={configApplied}, ScaleApplied={scaleApplied}, OverallSuccess={overallSuccess}");
            return overallSuccess;

        }

        public async Task<bool> ApplyPresetAsync(DisplayDeviceInfo device, DisplayPreset preset)
        {
             if (preset == null || device?.DeviceName == null) return false;

             Console.WriteLine($"Applicator: Attempting preset '{preset.Name}' ({preset.Parameters}) on device '{device.FriendlyName}'...");

            // 1. Check Resolution Compatibility
            var supportedModes = _infoService.GetSupportedModes(device.DeviceName).ToList();
            var modesWithTargetResolution = supportedModes
                .Where(m => m.Width == preset.Width && m.Height == preset.Height)
                .ToList();

            if (!modesWithTargetResolution.Any())
            {
                Console.WriteLine($"Applicator Error: Device '{device.FriendlyName}' does not support resolution {preset.Width}x{preset.Height}.");
                return false; // Cannot apply
            }

            // 2. Determine Target Refresh Rate (handle incompatibility)
            int targetRefreshRate = preset.RefreshRate;
            bool rateSupported = modesWithTargetResolution.Any(m => m.RefreshRate == targetRefreshRate);

            if (!rateSupported)
            {
                int bestAvailableRate = modesWithTargetResolution
                                        .OrderBy(m => Math.Abs(m.RefreshRate - targetRefreshRate))
                                        .First().RefreshRate;
                Console.WriteLine($"Applicator Warning: Preset refresh rate {targetRefreshRate}Hz not supported. Applying closest rate: {bestAvailableRate}Hz.");
                targetRefreshRate = bestAvailableRate; // Use supported rate
            }

            // 3. Create a temporary DisplayModeInfo for ApplySettingsAsync
            var targetResolutionInfo = new DisplayModeInfo
            {
                Width = preset.Width,
                Height = preset.Height,
                RefreshRate = targetRefreshRate // Use the determined rate
            };

            // 4. Call the specific ApplySettingsAsync method
            return await ApplySettingsAsync(device, targetResolutionInfo, targetRefreshRate, preset.Dpi);
        }
    }
}