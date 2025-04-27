using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.ViewModels.Display
{
    public class DisplaySettingsApplicator : IDisplaySettingsApplicator
    {
        private readonly IDisplayConfigService _configService;
        private readonly IDisplayScaleService _scaleService;
        private readonly IDisplayInfoService _infoService; // Needed for compatibility checks
        private IDisplaySettingsApplicator _displaySettingsApplicatorImplementation;

        public DisplaySettingsApplicator(
            IDisplayConfigService configService,
            IDisplayScaleService scaleService,
            IDisplayInfoService infoService) // Inject InfoService
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scaleService = scaleService ?? throw new ArgumentNullException(nameof(scaleService));
            _infoService = infoService ?? throw new ArgumentNullException(nameof(infoService)); // Assign InfoService
        }

        // --- MODIFIED ApplySettingsAsync Method ---
        /// <summary>
        /// Applies display settings using a DisplayConfigRequest object and a specific DPI value.
        /// </summary>
        /// <param name="device">The target device information (needed for scaling).</param>
        /// <param name="request">The configuration request containing resolution, refresh rate, orientation, etc.</param>
        /// <param name="dpi">The target DPI scaling value.</param>
        /// <returns>True if both config and scaling applied successfully (or didn't need changing), false otherwise.</returns>
        public async Task<bool> ApplySettingsAsync(DisplayDeviceInfo device, DisplayConfigRequest request, uint dpi)
        {
            // Validate input parameters
            if (device == null || request == null || string.IsNullOrEmpty(request.DeviceName))
            {
                 Console.WriteLine("Applicator Error: ApplySettingsAsync received invalid device or request.");
                 return false;
            }

            Console.WriteLine($"Applicator: Applying request to {device.FriendlyName ?? request.DeviceName}: " +
                              $"{request.Width}x{request.Height}, {request.RefreshRate}Hz, Orientation={request.Orientation?.ToString() ?? "Unchanged"}, DPI={dpi}%");

            // 1. Apply Configuration using the provided request object
            //    ASSUMPTION: _configService.ApplyDisplayConfiguration implementation has been updated
            //    to correctly handle all properties within DisplayConfigRequest, including Orientation.
            bool configApplied = false;
            try
            {
                configApplied = _configService.ApplyDisplayConfiguration(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Applicator: Error calling ApplyDisplayConfiguration for {request.DeviceName}: {ex.Message}");
                configApplied = false; // Ensure it's false on exception
            }


            // 2. Apply Scaling using the provided device info and dpi value
            bool scaleApplied = false;
            try
            {
                // Check if scaling needs to be applied
                var currentScaling = _scaleService.GetScalingInfo(device.AdapterId, device.SourceId);
                 if (currentScaling.IsInitialized && currentScaling.Current != dpi)
                 {
                     scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, dpi);
                 } else if (!currentScaling.IsInitialized) {
                     // Log a warning but still attempt to set DPI if info wasn't initialized
                     Console.WriteLine($"Applicator Warning: Could not get current scaling info for {device.FriendlyName}. Attempting to set DPI {dpi}% anyway.");
                     scaleApplied = _scaleService.SetScaling(device.AdapterId, device.SourceId, dpi);
                 } else {
                     scaleApplied = true; // DPI was already correct, consider it successful in terms of matching target
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Applicator: Error applying scaling ({dpi}%) to {device.FriendlyName}: {ex.Message}");
                scaleApplied = false; // Ensure it's false on exception
            }

            // 3. Determine overall success
            //    Consider success only if both config and scaling were successfully applied
            //    (or if scaling didn't need changing and config was applied).
            bool overallSuccess = configApplied && scaleApplied;
            Console.WriteLine($"Applicator: ApplySettings result for {device.FriendlyName} - ConfigApplied={configApplied}, ScaleApplied={scaleApplied}, OverallSuccess={overallSuccess}");

            // Optional: Add a small delay AFTER applying settings before returning,
            // allowing the system potentially more time to settle before the UI re-queries state.
            // await Task.Delay(100); // e.g., 100ms delay

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

            // 3. Create the DisplayConfigRequest from the preset and determined rate
            var request = new DisplayConfigRequest
            {
                DeviceName = device.DeviceName,
                Width = preset.Width,
                Height = preset.Height,
                RefreshRate = targetRefreshRate,
                Orientation = null
            };

            // 4. Call the modified ApplySettingsAsync, passing the request and preset DPI
            return await ApplySettingsAsync(device, request, preset.Dpi);
        }
    }
}