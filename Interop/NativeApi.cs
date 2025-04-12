using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessWindowApp.Interop
{
    public static class NativeApi
    {
        // Constants for GetWindowLong/SetWindowLong
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        #region Delegates

        /// <summary>
        /// 回调函数，用于枚举所有顶级窗口
        /// </summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region P/Invoke - Window Enumeration & Title

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        #endregion

        #region P/Invoke - Window Size & Position

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        #endregion
        
        #region P/Invoke - MonitorInfo
        
        // Monitor APIs
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        /// <summary>
        /// 包含屏幕信息：屏幕范围与工作区域（排除任务栏）
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;  // 整个显示器的矩形区域
            public RECT rcWork;     // 工作区区域（减去任务栏）
            public uint dwFlags;
        }
        
        #endregion

        #region P/Invoke - Style

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public Rectangle ToRectangle() =>
                new Rectangle(Left, Top, Right - Left, Bottom - Top);
        }

        #endregion
    }
}
