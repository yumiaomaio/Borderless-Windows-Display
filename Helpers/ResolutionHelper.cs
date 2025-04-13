using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Helpers
{
    public static class ResolutionHelper
    {
        public static List<(int width, int height, int frequency)> GetAllSupportedModes(string deviceName)
        {
            List<(int, int, int)> modes = new();
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
            int modeIndex = 0;

            while (DisplayConfigApi.EnumDisplaySettings(deviceName, modeIndex, ref devMode))
            {
                modes.Add(( (int)devMode.dmPelsWidth, (int)devMode.dmPelsHeight, (int)devMode.dmDisplayFrequency ));
                modeIndex++;
            }

            return modes.Distinct().ToList();
        }

        public static List<string> GetAllDisplayDeviceNames()
        {
            List<string> names = new();
            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);

            uint devNum = 0;
            while (DisplayConfigApi.EnumDisplayDevices(null, devNum++, ref d, 0))
            {
                if ((d.StateFlags & 0x00000001) != 0) // DISPLAY_DEVICE_ACTIVE
                {
                    names.Add(d.DeviceName); // like \\.\DISPLAY1
                }

                d.cb = Marshal.SizeOf(d); // reset for next
            }

            return names;
        }

        public static bool TryChangeResolution(string deviceName, int width, int height, int frequency)
        {
            
            
            var devMode = new DEVMODE
            {
                dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)),
                dmPelsWidth = (uint)width,
                dmPelsHeight = (uint)height,
                dmDisplayFrequency = (uint)frequency,
                dmFields = (uint)(DEVMODEFields.PelsWidth | DEVMODEFields.PelsHeight | DEVMODEFields.DisplayFrequency)
            };

            var result = DisplayConfigApi.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, 0x01, IntPtr.Zero); // CDS_UPDATEREGISTRY
            Console.WriteLine($"ChangeDisplaySettingsEx 返回值: {result}");
            return result == 0;
        }
        
        public static (int width, int height, int frequency)? GetCurrentDisplayMode(string deviceName)
        {
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
            const int ENUM_CURRENT_SETTINGS = -1;

            if (DisplayConfigApi.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                return ((int)devMode.dmPelsWidth, (int)devMode.dmPelsHeight, (int)devMode.dmDisplayFrequency);
            }

            return null;
        }
        
    }
}
