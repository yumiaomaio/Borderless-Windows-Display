﻿using BorderlessWindowApp.Services.Display.Models;
using BorderlessWindowApp.Services.Presets;

// For DisplayPreset

namespace BorderlessWindowApp.ViewModels.Display
{
    public interface IDisplaySettingsApplicator
    {
        Task<bool> ApplySettingsAsync(DisplayDeviceInfo device, DisplayConfigRequest request, uint dpi);
        Task<bool> ApplyPresetAsync(DisplayDeviceInfo device, DisplayPreset preset);
    }
}