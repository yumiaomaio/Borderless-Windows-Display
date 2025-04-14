using System;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;

namespace BorderlessWindowApp.Services
{
    /// <summary>
    /// 提供对 WinEvent 和 CBT 系统钩子的操作接口。
    /// 支持窗口行为的监听与拦截。
    /// </summary>
    public interface IWindowHookService
    {
        /// <summary>
        /// 安装 WinEvent 钩子，监听窗口事件变化。
        /// </summary>
        /// <param name="target">目标窗口句柄</param>
        /// <param name="onEvent">回调触发函数（可用于日志）</param>
        void AttachWinEventHook(IntPtr target, Action<string> onEvent);

        /// <summary>
        /// 卸载所有 WinEvent 钩子
        /// </summary>
        void DetachWinEventHooks();

        /// <summary>
        /// 安装 CBT 钩子，可用于拦截窗口行为（如最小化、移动）
        /// </summary>
        /// <param name="target">目标窗口句柄</param>
        /// <param name="onIntercept">可选回调，提供钩子事件代码和参数</param>
        void AttachCbtHook(IntPtr target, Action<CBTHookCode, IntPtr, IntPtr>? onIntercept = null);

        /// <summary>
        /// 卸载 CBT 钩子
        /// </summary>
        void DetachCbtHook();
    }
}