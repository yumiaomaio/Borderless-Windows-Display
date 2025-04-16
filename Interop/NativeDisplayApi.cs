using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display;

namespace BorderlessWindowApp.Interop
{
    public static class NativeDisplayApi
    {
        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(
            uint flags, out uint numPathArrayElements,
            out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetDisplayConfig(
            int numPathArrayElements,
            [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
            int numModeInfoArrayElements,
            [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            SetDisplayConfigFlags flags);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplaySettings(
            string deviceName,
            int modeNum,
            ref DEVMODE devMode);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(
            string lpDevice, uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(
            ref DISPLAYCONFIG_GET_DPI header);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigSetDeviceInfo(
            ref DISPLAYCONFIG_SET_DPI header);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ChangeDisplaySettingsEx(
            string? lpszDeviceName,
            ref DEVMODE lpDevMode,
            IntPtr hwnd,
            int dwflags,
            IntPtr lParam);
        
        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(
            int flags,
            ref uint numPathArrayElements,
            ref uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(
            int flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathInfoArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(
            ref DISPLAYCONFIG_TARGET_DEVICE_NAME request);

    }
}