using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Models;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public class DisplayInfoService : IDisplayInfoService
    {
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
                }
                d.cb = Marshal.SizeOf(d);
            }

            return names;
        }

        public IEnumerable<DisplayModeInfo> GetSupportedModes(string deviceName)
        {
            List<DisplayModeInfo> modes = new();
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };

            int i = 0;
            while (NativeDisplayApi.EnumDisplaySettings(deviceName, i++, ref devMode))
            {
                modes.Add(new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                });
            }

            return modes.DistinctBy(m => (m.Width, m.Height, m.RefreshRate)).ToList();
        }

        public DisplayModeInfo? GetCurrentMode(string deviceName)
        {
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
            const int ENUM_CURRENT_SETTINGS = -1;

            if (NativeDisplayApi.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                return new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                };
            }

            return null;
        }
    }
}
