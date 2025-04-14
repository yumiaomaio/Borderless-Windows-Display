using System;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services
{
    public class WindowManagerService : IWindowManagerService
    {
        private readonly ILogger<WindowManagerService> _logger;

        public WindowManagerService(ILogger<WindowManagerService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 激活并置顶窗口（如果最小化则先还原）
        /// </summary>
        public void FocusWindow(IntPtr hwnd)
        {
            if (!NativeWindowApi.IsWindow(hwnd)) return;

            if (NativeWindowApi.IsIconic(hwnd))
            {
                _logger.LogDebug("窗口处于最小化状态，尝试还原：{Handle}", hwnd);
                NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.Restore);
            }

            NativeWindowApi.SetForegroundWindow(hwnd);
            NativeWindowApi.BringWindowToTop(hwnd);
            _logger.LogInformation("已激活窗口：{Handle}", hwnd);
        }

        /// <summary>
        /// 最小化窗口
        /// </summary>
        public void MinimizeWindow(IntPtr hwnd)
        {
            NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.Minimize);
            _logger.LogInformation("窗口已最小化：{Handle}", hwnd);
        }

        /// <summary>
        /// 最大化窗口
        /// </summary>
        public void MaximizeWindow(IntPtr hwnd)
        {
            NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.ShowMaximized);
            _logger.LogInformation("窗口已最大化：{Handle}", hwnd);
        }

        /// <summary>
        /// 恢复窗口（从最小化或最大化状态）
        /// </summary>
        public void RestoreWindow(IntPtr hwnd)
        {
            NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.Restore);
            _logger.LogInformation("窗口已恢复：{Handle}", hwnd);
        }

        /// <summary>
        /// 隐藏窗口（不会关闭）
        /// </summary>
        public void HideWindow(IntPtr hwnd)
        {
            NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.Hide);
            _logger.LogInformation("窗口已隐藏：{Handle}", hwnd);
        }

        /// <summary>
        /// 显示窗口（用于重新展示隐藏的窗口）
        /// </summary>
        public void ShowWindow(IntPtr hwnd)
        {
            NativeWindowApi.ShowWindow(hwnd, (int)ShowWindowCommand.Show);
            _logger.LogInformation("窗口已显示：{Handle}", hwnd);
        }

        /// <summary>
        /// 将窗口发送到底层（其他窗口之下）
        /// </summary>
        public void SendToBack(IntPtr hwnd)
        {
            NativeWindowApi.SetWindowPos(
                hwnd,
                new IntPtr(1), // HWND_BOTTOM
                0, 0, 0, 0,
                (uint)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE)
            );
            _logger.LogInformation("窗口已置底：{Handle}", hwnd);
        }
    }
}
