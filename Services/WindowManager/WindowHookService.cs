using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Delegates;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Hook;
using BorderlessWindowApp.Interop.Enums.Window;
using BorderlessWindowApp.Services.Window;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services
{
    public class WindowHookService : IWindowHookService
    {
        private readonly ILogger<WindowHookService> _logger;

        // WinEvent hook
        private WinEventDelegate? _eventCallback;
        private readonly List<IntPtr> _eventHooks = new();

        // CBT hook
        private HookProc? _cbtProc;
        private IntPtr _cbtHook = IntPtr.Zero;
        private IntPtr _targetHwnd = IntPtr.Zero;
        private Action<CBTHookCode, IntPtr, IntPtr>? _interceptHandler;

        public WindowHookService(ILogger<WindowHookService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 附加 WinEvent 钩子到目标窗口，捕捉系统事件如移动、最小化等
        /// </summary>
        public void AttachWinEventHook(IntPtr target, Action<string> onEvent)
        {
            _eventCallback = (hWinEventHook, eventType, hwnd, idObject, idChild, threadId, timestamp) =>
            {
                if (hwnd == target)
                {
                    string name = Enum.GetName(typeof(WinEventType), eventType) ?? $"UNKNOWN_EVENT_{eventType:X}";
                    onEvent?.Invoke($"[WIN_EVENT] {name} | HWND: {hwnd}");
                    _logger.LogDebug("WinEvent triggered: {EventType} on {Handle}", name, hwnd);
                }
            };

            WinEventType[] events = {
                WinEventType.Foreground,
                WinEventType.MinimizeStart,
                WinEventType.MinimizeEnd,
                WinEventType.Show,
                WinEventType.Hide,
                WinEventType.LocationChange
            };

            foreach (var evt in events)
            {
                IntPtr hook = NativeWindowApi.SetWinEventHook((uint)evt, (uint)evt, IntPtr.Zero, _eventCallback, 0, 0, 0);
                if (hook != IntPtr.Zero)
                {
                    _eventHooks.Add(hook);
                    _logger.LogInformation("WinEventHook set for event {EventType}", evt);
                }
            }
        }

        public void DetachWinEventHooks()
        {
            foreach (var hook in _eventHooks)
            {
                NativeWindowApi.UnhookWinEvent(hook);
                _logger.LogInformation("WinEventHook removed: {Hook}", hook);
            }
            _eventHooks.Clear();
        }


        /// <summary>
        /// 附加 CBT 钩子以拦截窗口行为，例如阻止最小化、移动等
        /// </summary>
        public void AttachCbtHook(IntPtr target, Action<CBTHookCode, IntPtr, IntPtr>? onIntercept = null)
        {
            _targetHwnd = target;
            _interceptHandler = onIntercept;

            _cbtProc = (nCode, wParam, lParam) =>
            {
                var code = (CBTHookCode)nCode;

                // 示例：阻止最小化
                if (code == CBTHookCode.HCBT_MINMAX && wParam == _targetHwnd)
                {
                    _logger.LogWarning("CBT 拦截最小化：{Handle}", wParam);
                    _interceptHandler?.Invoke(code, wParam, lParam);
                    return 1; // 非 0 表示拦截
                }

                // 示例：阻止窗口移动（HCBT_MOVESIZE）
                if (code == CBTHookCode.HCBT_MOVESIZE && wParam == _targetHwnd)
                {
                    _logger.LogWarning("CBT 拦截移动/调整大小：{Handle}", wParam);
                    _interceptHandler?.Invoke(code, wParam, lParam);
                    return 1;
                }

                return NativeWindowApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            };

            _cbtHook = NativeWindowApi.SetWindowsHookEx(
                (int)WindowsHookType.WH_CBT,
                _cbtProc,
                IntPtr.Zero,
                NativeWindowApi.GetCurrentThreadId());

            _logger.LogInformation("CBT hook attached to thread for target: {Handle}", target);
        }

        public void DetachCbtHook()
        {
            if (_cbtHook != IntPtr.Zero)
            {
                NativeWindowApi.UnhookWindowsHookEx(_cbtHook);
                _logger.LogInformation("CBT hook removed for target: {Handle}", _targetHwnd);
                _cbtHook = IntPtr.Zero;
            }
        }
    }
}