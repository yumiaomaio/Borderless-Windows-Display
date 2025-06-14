﻿using System.Collections.Generic;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Models;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public interface IDisplayInfoService
    {
        IEnumerable<string> GetAllDeviceNames();
        IEnumerable<DisplayModeInfo> GetSupportedModes(string deviceName);
        DisplayModeInfo? GetCurrentMode(string deviceName);
        List<DisplayDeviceInfo> GetAllDisplayDevices();
    }
}