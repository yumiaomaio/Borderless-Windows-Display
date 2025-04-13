using System.Drawing;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class WindowSizeHelper
    {
        public static Rectangle GetWindowRect(IntPtr hWnd)
        {
            if (!Win32WindowApi.GetWindowRect(hWnd, out RECT rect))
                return Rectangle.Empty;

            return new Rectangle(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top);
        }

        public static Size GetSize(IntPtr hWnd)
        {
            var r = GetWindowRect(hWnd);
            return new Size(r.Width, r.Height);
        }

        public static Rectangle GetClientRect(IntPtr hWnd)
        {
            if (!Win32WindowApi.GetClientRect(hWnd, out RECT client))
                return Rectangle.Empty;

            return new Rectangle(
                client.Left,
                client.Top,
                client.Right - client.Left,
                client.Bottom - client.Top);
        }

        public static void SetWindowRect(IntPtr hWnd, Rectangle rect)
        {
            Win32WindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                rect.X, rect.Y,
                rect.Width, rect.Height,
                (uint)(SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_FRAMECHANGED));
        }

        public static void SetClientSize(IntPtr hWnd, int width, int height, bool centerOnScreen = false)
        {
            // 获取当前窗口样式
            var style = Win32WindowApi.GetWindowLong(hWnd, -16);     // GWL_STYLE
            var exStyle = Win32WindowApi.GetWindowLong(hWnd, -20);   // GWL_EXSTYLE

            var rect = new RECT { Left = 0, Top = 0, Right = width, Bottom = height };
            Win32WindowApi.AdjustWindowRectEx(ref rect, (uint)style, false, (uint)exStyle);

            int finalWidth = rect.Right - rect.Left;
            int finalHeight = rect.Bottom - rect.Top;

            int x = 100, y = 100; // 默认位置
            if (centerOnScreen)
            {
                var screen = System.Windows.SystemParameters.WorkArea;
                x = (int)(screen.Width / 2 - finalWidth / 2);
                y = (int)(screen.Height / 2 - finalHeight / 2);
            }

            Win32WindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                x, y,
                finalWidth, finalHeight,
                (uint)(SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_FRAMECHANGED));
        }
    }
}
