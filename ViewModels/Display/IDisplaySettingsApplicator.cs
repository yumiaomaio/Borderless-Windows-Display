using BorderlessWindowApp.Services.Display.Models;

// For DisplayPreset

namespace BorderlessWindowApp.ViewModels.Display
{
    public interface IDisplaySettingsApplicator
    {
        /// <summary>
        /// Applies specific display settings to a device.
        /// </summary>
        /// <returns>True if both config and scaling applied successfully (or didn't need changing), false otherwise.</returns>
        Task<bool> ApplySettingsAsync(DisplayDeviceInfo device, DisplayModeInfo resolution, int refreshRate, uint dpi);

        /// <summary>
        /// Applies a preset to a device, handling compatibility checks.
        /// </summary>
        /// <returns>True if preset applied successfully (potentially with adjustments), false otherwise.</returns>
        Task<bool> ApplyPresetAsync(DisplayDeviceInfo device, DisplayPreset preset);
    }
}