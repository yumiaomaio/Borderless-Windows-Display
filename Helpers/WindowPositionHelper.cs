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
            return Win32WindowApi.SetWindowPos(hWnd, IntPtr.Zero,
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
        public static void CenterWindowToScreen(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return;

            // 获取窗口大小
            Rectangle windowRect = WindowSizeHelper.GetWindowRect(hWnd);
            int winWidth = windowRect.Width;
            int winHeight = windowRect.Height;

            // 获取当前窗口所在屏幕的工作区
            Rectangle workArea = ScreenHelper.GetWorkAreaFromWindow(hWnd);

            int x = workArea.X + (workArea.Width - winWidth) / 2;
            int y = workArea.Y + (workArea.Height - winHeight) / 2;

            Win32WindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                x, y, 0, 0,
                (uint)(
                    SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_NOACTIVATE));
        }
        
    }
}