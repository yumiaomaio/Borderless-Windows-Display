using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    /// <summary>
    /// 提供显示设备信息服务，包括设备枚举、支持模式查询和当前模式获取。
    /// </summary>
    public class DisplayInfoService : IDisplayInfoService
    {
        private readonly ILogger<DisplayInfoService> _logger;

        public DisplayInfoService(ILogger<DisplayInfoService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取所有激活的显示设备名称。
        /// </summary>
        public IEnumerable<string> GetAllDeviceNames()
        {
            List<string> names = new();
            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);

            uint devNum = 0;
            while (NativeDisplayApi.EnumDisplayDevices(null, devNum++, ref d, 0))
            {
                if ((d.StateFlags & 0x00000001) != 0) // DISPLAY_DEVICE_ACTIVE
                {
                    names.Add(d.DeviceName); // "\\.\DISPLAY1"
                    _logger.LogDebug("Found active display device: {DeviceName}", d.DeviceName);
                }
                d.cb = Marshal.SizeOf(d);
            }

            _logger.LogInformation("Total active display devices found: {Count}", names.Count);
            return names;
        }

        /// <summary>
        /// 获取指定显示设备支持的所有显示模式（分辨率 + 刷新率）。
        /// </summary>
        public IEnumerable<DisplayModeInfo> GetSupportedModes(string deviceName)
        {
            List<DisplayModeInfo> modes = new();
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };

            int i = 0;
            while (NativeDisplayApi.EnumDisplaySettings(deviceName, i++, ref devMode))
            {
                var mode = new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                };
                modes.Add(mode);
                _logger.LogDebug("Mode found for {Device}: {Width}x{Height} @ {Hz}Hz", deviceName, mode.Width, mode.Height, mode.RefreshRate);
            }

            _logger.LogInformation("Total display modes found for {Device}: {Count}", deviceName, modes.Count);
            return modes.DistinctBy(m => (m.Width, m.Height, m.RefreshRate)).ToList();
        }

        /// <summary>
        /// 获取显示设备当前正在使用的显示模式。
        /// </summary>
        public DisplayModeInfo? GetCurrentMode(string deviceName)
        {
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
            const int ENUM_CURRENT_SETTINGS = -1;

            if (NativeDisplayApi.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                var mode = new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                };
                _logger.LogInformation("Current mode for {Device}: {Width}x{Height} @ {Hz}Hz", deviceName, mode.Width, mode.Height, mode.RefreshRate);
                return mode;
            }

            _logger.LogWarning("Failed to get current mode for device: {Device}", deviceName);
            return null;
        }
    }
}
