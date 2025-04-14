using System;

namespace BorderlessWindowApp.Services
{
    /// <summary>
    /// 提供基础的窗口控制操作，如激活、隐藏、最小化等。
    /// 所有方法基于窗口句柄（HWND）操作。
    /// </summary>
    public interface IWindowManagerService
    {
        /// <summary>
        /// 激活并置顶窗口（如果最小化则先还原）
        /// </summary>
        void FocusWindow(IntPtr hwnd);

        /// <summary>
        /// 最小化窗口
        /// </summary>
        void MinimizeWindow(IntPtr hwnd);

        /// <summary>
        /// 最大化窗口
        /// </summary>
        void MaximizeWindow(IntPtr hwnd);

        /// <summary>
        /// 恢复窗口（从最小化或最大化状态）
        /// </summary>
        void RestoreWindow(IntPtr hwnd);

        /// <summary>
        /// 隐藏窗口（不会关闭）
        /// </summary>
        void HideWindow(IntPtr hwnd);

        /// <summary>
        /// 显示窗口（用于重新展示隐藏的窗口）
        /// </summary>
        void ShowWindow(IntPtr hwnd);

        /// <summary>
        /// 将窗口发送到底层（其他窗口之下）
        /// </summary>
        void SendToBack(IntPtr hwnd);
    }
}