using System.Drawing;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Helpers
{
    public static class WindowPositionHelper
    {
        /// <summary>
        /// 获取用于 SetWindowPos 的默认标志位（如果无矩形则跳过尺寸和位置）
        /// </summary>
        public static SetWindowPosFlags GetDefaultFlags(Rectangle rect)
        {
            var flags = SetWindowPosFlags.SWP_FRAMECHANGED |
                        SetWindowPosFlags.SWP_NOOWNERZORDER |
                        SetWindowPosFlags.SWP_NOZORDER;

            if (rect.IsEmpty)
                flags |= SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE;

            return flags;
        }
        
        /// <summary>
        /// 设置窗口的完整矩形位置（含边框）
        /// </summary>
        public static bool SetWindowRect(IntPtr hWnd, Rectangle rect, SetWindowPosFlags flags)
        {
            return NativeApi.SetWindowPos(hWnd, IntPtr.Zero,
                rect.X, rect.Y, rect.Width, rect.Height, (uint)flags);
        }

        /// <summary>
        /// 移动窗口到指定位置（不改变大小）
        /// </summary>
        public static bool MoveWindow(IntPtr hWnd, int x, int y)
        {
            var rect = WindowSizeHelper.GetWindowRect(hWnd);
            return SetWindowRect(hWnd, new Rectangle(x, y, rect.Width, rect.Height),
                SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER);
        }

        /// <summary>
        /// 将窗口居中到当前屏幕（默认使用工作区）
        /// </summary>
        public static bool CenterToScreen(IntPtr hWnd, bool excludeTaskbar = true)
        {
            var windowRect = WindowSizeHelper.GetWindowRect(hWnd);
            if (windowRect.IsEmpty) return false;

            Rectangle screenArea = excludeTaskbar
                ? WindowSizeHelper.GetWorkingArea(hWnd)
                : WindowSizeHelper.GetScreenBounds(hWnd);

            if (screenArea.IsEmpty) return false;

            int x = screenArea.X + (screenArea.Width - windowRect.Width) / 2;
            int y = screenArea.Y + (screenArea.Height - windowRect.Height) / 2;

            return SetWindowRect(hWnd, new Rectangle(x, y, windowRect.Width, windowRect.Height),
                SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_SHOWWINDOW);
        }
        
    }
}