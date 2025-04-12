using System.Drawing;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Helpers
{
    public static class WindowSizeHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        public static Rectangle GetWindowRect(IntPtr hWnd)
        {
            return NativeApi.GetWindowRect(hWnd, out var rect) ? rect.ToRectangle() : Rectangle.Empty;
        }

        public static Rectangle GetWorkingArea(IntPtr hWnd)
        {
            IntPtr monitor = NativeApi.MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            var info = new NativeApi.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeApi.MONITORINFO>() };

            return NativeApi.GetMonitorInfo(monitor, ref info) ? info.rcWork.ToRectangle() : Rectangle.Empty;
        }

        public static Rectangle GetScreenBounds(IntPtr hWnd)
        {
            IntPtr monitor = NativeApi.MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            var info = new NativeApi.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeApi.MONITORINFO>() };

            return NativeApi.GetMonitorInfo(monitor, ref info) ? info.rcMonitor.ToRectangle() : Rectangle.Empty;
        }

        public static bool SetClientSize(IntPtr hWnd, int clientWidth, int clientHeight, bool centerOnScreen = false)
        {
            if (hWnd == IntPtr.Zero || !NativeApi.GetWindowRect(hWnd, out var currentRect)) return false;

            int style = NativeApi.GetWindowLong(hWnd, NativeApi.GWL_STYLE);
            int exStyle = NativeApi.GetWindowLong(hWnd, NativeApi.GWL_EXSTYLE);

            var adjustedRect = new NativeApi.RECT
            {
                Left = 0,
                Top = 0,
                Right = clientWidth,
                Bottom = clientHeight
            };

            if (!NativeApi.AdjustWindowRectEx(ref adjustedRect, (uint)style, false, (uint)exStyle))
                return false;

            int newWidth = adjustedRect.Right - adjustedRect.Left;
            int newHeight = adjustedRect.Bottom - adjustedRect.Top;

            int x = currentRect.Left;
            int y = currentRect.Top;

            if (centerOnScreen)
            {
                Rectangle screen = GetWorkingArea(hWnd);
                x = screen.X + (screen.Width - newWidth) / 2;
                y = screen.Y + (screen.Height - newHeight) / 2;
            }

            return NativeApi.SetWindowPos(hWnd, IntPtr.Zero, x, y, newWidth, newHeight,
                (uint)(SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_SHOWWINDOW));
        }
    }
}
