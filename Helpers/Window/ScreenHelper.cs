using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class ScreenHelper
    {
        public static Rectangle GetPrimaryScreenBounds()
        {
            return new Rectangle(
                0,
                0,
                (int)SystemParameters.PrimaryScreenWidth,
                (int)SystemParameters.PrimaryScreenHeight);
        }

        public static Rectangle GetPrimaryWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            return new Rectangle(
                (int)wa.Left,
                (int)wa.Top,
                (int)wa.Width,
                (int)wa.Height);
        }

        public static Rectangle GetScreenFromWindow(IntPtr hWnd)
        {
            var monitor = Win32WindowApi.MonitorFromWindow(hWnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            if (Win32WindowApi.GetMonitorInfo(monitor, ref info))
            {
                var r = info.rcMonitor;
                return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
            }

            return GetPrimaryScreenBounds();
        }

        public static Rectangle GetWorkAreaFromWindow(IntPtr hWnd)
        {
            var monitor = Win32WindowApi.MonitorFromWindow(hWnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            if (Win32WindowApi.GetMonitorInfo(monitor, ref info))
            {
                var r = info.rcWork;
                return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
            }

            return GetPrimaryWorkArea();
        }
    }
}