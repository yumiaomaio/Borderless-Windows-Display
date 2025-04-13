using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;
using Size = System.Drawing.Size;

namespace BorderlessWindowApp.Services.WindowLayout
{
    
    public class WindowLayoutService
    {
        public Rectangle GetWindowRect(IntPtr hWnd)
        {
            if (!NativeWindowApi.GetWindowRect(hWnd, out RECT rect))
                return Rectangle.Empty;

            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public Size GetSize(IntPtr hWnd) => GetWindowRect(hWnd).Size;

        public Rectangle GetClientRect(IntPtr hWnd)
        {
            if (!NativeWindowApi.GetClientRect(hWnd, out RECT rect))
                return Rectangle.Empty;

            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }


        public void SetWindowLayout(IntPtr hWnd, Size size, WindowLayoutOptions options)
        {
            if (hWnd == IntPtr.Zero || size.IsEmpty)
                return;

            // 1. 将客户区大小转换为窗口大小（如果启用）
            int finalWidth = size.Width;
            int finalHeight = size.Height;

            if (options.UseClientSize)
            {
                var style = NativeWindowApi.GetWindowLong(hWnd, -16);   // GWL_STYLE
                var exStyle = NativeWindowApi.GetWindowLong(hWnd, -20); // GWL_EXSTYLE

                var rect = new RECT { Left = 0, Top = 0, Right = size.Width, Bottom = size.Height };
                NativeWindowApi.AdjustWindowRectEx(ref rect, (uint)style, false, (uint)exStyle);

                finalWidth = rect.Right - rect.Left;
                finalHeight = rect.Bottom - rect.Top;
            }

            // 2. 计算窗口位置
            int x = 100, y = 100; // 默认初始位置

            if (options.CenterToScreen)
            {
                var workArea = GetMonitorWorkArea(hWnd, options.MonitorTarget, options.MonitorIndex);
                x = workArea.X + (workArea.Width - finalWidth) / 2;
                y = workArea.Y + (workArea.Height - finalHeight) / 2;
            }
            else if (options.Location.HasValue)
            {
                x = options.Location.Value.X;
                y = options.Location.Value.Y;
            }

            // 3. 最终设置窗口位置和大小
            NativeWindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                x, y,
                finalWidth, finalHeight,
                (uint)options.Flags);
        }
        
        private Rectangle GetMonitorWorkArea(IntPtr hWnd, RelativeMonitor monitor, int index = 0)
        {
            if (monitor == RelativeMonitor.Primary)
            {
                var wa = SystemParameters.WorkArea;
                return new Rectangle((int)wa.Left, (int)wa.Top, (int)wa.Width, (int)wa.Height);
            }

            IntPtr hMonitor = NativeWindowApi.MonitorFromWindow(hWnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            if (NativeWindowApi.GetMonitorInfo(hMonitor, ref info))
            {
                return monitor switch
                {
                    RelativeMonitor.Current => ConvertRECT(info.rcWork),
                    RelativeMonitor.Specific => ConvertRECT(info.rcWork), // 暂时简化，未来可改成通过 index 查找
                    _ => ConvertRECT(info.rcWork)
                };
            }

            var fallback = SystemParameters.WorkArea;
            return new Rectangle((int)fallback.Left, (int)fallback.Top, (int)fallback.Width, (int)fallback.Height);
        }

        private Rectangle ConvertRECT(RECT r)
        {
            return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

    }
    
}
