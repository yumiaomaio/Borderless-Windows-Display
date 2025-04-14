using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;
using Microsoft.Extensions.Logging;
using Size = System.Drawing.Size;

namespace BorderlessWindowApp.Services.WindowLayout
{
    public class WindowLayoutService : IWindowLayoutService
    {
        private readonly ILogger<WindowLayoutService> _logger;

        public WindowLayoutService(ILogger<WindowLayoutService> logger)
        {
            _logger = logger;
        }

        public Rectangle GetWindowRect(IntPtr hWnd)
        {
            // 获取窗口的整体矩形（包括边框和标题栏）
            if (!NativeWindowApi.GetWindowRect(hWnd, out RECT rect))
            {
                _logger.LogWarning("Failed to get window rect for hWnd: {Handle}", hWnd);
                return Rectangle.Empty;
            }

            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public Rectangle GetClientRect(IntPtr hWnd)
        {
            // 获取窗口的客户区矩形（不包含边框和标题栏）
            if (!NativeWindowApi.GetClientRect(hWnd, out RECT rect))
            {
                _logger.LogWarning("Failed to get client rect for hWnd: {Handle}", hWnd);
                return Rectangle.Empty;
            }

            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public void SetWindowLayout(IntPtr hWnd, Size size, WindowLayoutOptions options)
        {
            // 校验句柄与尺寸是否有效
            if (hWnd == IntPtr.Zero || size.IsEmpty)
            {
                _logger.LogWarning("Invalid parameters in SetWindowLayout: hWnd={Handle}, size={Size}", hWnd, size);
                return;
            }

            int finalWidth = size.Width;
            int finalHeight = size.Height;

            // 如果启用客户区尺寸转换，调整为包含边框和标题栏的实际窗口大小
            if (options.UseClientSize)
            {
                var style = NativeWindowApi.GetWindowLong(hWnd, -16); // GWL_STYLE
                var exStyle = NativeWindowApi.GetWindowLong(hWnd, -20); // GWL_EXSTYLE

                var rect = new RECT { Left = 0, Top = 0, Right = size.Width, Bottom = size.Height };
                NativeWindowApi.AdjustWindowRectEx(ref rect, (uint)style, false, (uint)exStyle);

                finalWidth = rect.Right - rect.Left;
                finalHeight = rect.Bottom - rect.Top;
                _logger.LogDebug("Adjusted window size: {Width}x{Height}", finalWidth, finalHeight);
            }

            // 默认窗口位置
            int x = 100, y = 100;

            // 如果要求居中显示，则忽略 Location
            if (options.CenterToScreen)
            {
                var workArea = GetMonitorWorkArea(hWnd, options.MonitorTarget, options.MonitorIndex);
                x = workArea.X + (workArea.Width - finalWidth) / 2;
                y = workArea.Y + (workArea.Height - finalHeight) / 2;
                _logger.LogDebug("Centering window to screen: x={X}, y={Y}", x, y);
            }
            // 否则使用指定位置
            else if (options.Location.HasValue)
            {
                x = options.Location.Value.X;
                y = options.Location.Value.Y;
                _logger.LogDebug("Setting window location: x={X}, y={Y}", x, y);
            }

            // 调用 Windows API 设置窗口位置与大小
            bool result = NativeWindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                x, y,
                finalWidth, finalHeight,
                (uint)options.Flags);

            // 根据结果记录日志
            if (!result)
            {
                _logger.LogError("SetWindowPos failed for hWnd: {Handle}", hWnd);
            }
            else
            {
                _logger.LogInformation("Window layout set successfully for hWnd: {Handle}", hWnd);
            }
        }

        private Rectangle GetMonitorWorkArea(IntPtr hWnd, RelativeMonitor monitor, int index = 0)
        {
            // 如果目标是主显示器，直接获取系统工作区
            if (monitor == RelativeMonitor.Primary)
            {
                var wa = SystemParameters.WorkArea;
                return new Rectangle((int)wa.Left, (int)wa.Top, (int)wa.Width, (int)wa.Height);
            }

            // 否则获取与窗口最近的显示器句柄
            IntPtr hMonitor = NativeWindowApi.MonitorFromWindow(hWnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            // 获取显示器信息，成功则返回其工作区
            if (NativeWindowApi.GetMonitorInfo(hMonitor, ref info))
            {
                var rect = ConvertRECT(info.rcWork);
                _logger.LogDebug("Monitor work area: {Rect}", rect);
                return rect;
            }

            // 获取失败时回退使用系统默认工作区
            var fallback = SystemParameters.WorkArea;
            _logger.LogWarning("Fallback to SystemParameters.WorkArea for hWnd: {Handle}", hWnd);
            return new Rectangle((int)fallback.Left, (int)fallback.Top, (int)fallback.Width, (int)fallback.Height);
        }

        private Rectangle ConvertRECT(RECT r)
        {
            // 将 Win32 RECT 结构转换为 .NET Rectangle
            return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
    }
}
