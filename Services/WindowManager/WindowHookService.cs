using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Delegates;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Services.Window;

namespace BorderlessWindowApp.Services
{
    public class WindowHookService : IWindowHookService
    {
        // WinEvent hook
        private WinEventDelegate? _eventCallback;
        private readonly List<IntPtr> _eventHooks = new();

        // CBT hook
        private HookProc? _cbtProc;
        private IntPtr _cbtHook = IntPtr.Zero;
        private IntPtr _targetHwnd = IntPtr.Zero;

        public void AttachWinEventHook(IntPtr target, Action<string> onEvent)
        {
            _eventCallback = (hWinEventHook, eventType, hwnd, idObject, idChild, threadId, timestamp) =>
            {
                if (hwnd == target)
                {
                    string name = EventName(eventType);
                    onEvent?.Invoke($"[WIN_EVENT] {name} | HWND: {hwnd}");
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
                    _eventHooks.Add(hook);
            }
        }

        public void DetachWinEventHooks()
        {
            foreach (var hook in _eventHooks)
            {
                NativeWindowApi.UnhookWinEvent(hook);
            }
            _eventHooks.Clear();
        }

        public void AttachCbtHook(IntPtr target, Action<string>? onIntercept = null)
        {
            _targetHwnd = target;

            _cbtProc = (nCode, wParam, lParam) =>
            {
                if ((CBTHookCode)nCode == CBTHookCode.HCBT_MINMAX && wParam == _targetHwnd)
                {
                    onIntercept?.Invoke($"[CBT_BLOCK] 阻止窗口最小化：{wParam}");
                    return 1; // 阻止最小化
                }

                return NativeWindowApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            };

            _cbtHook = NativeWindowApi.SetWindowsHookEx(
                (int)WindowsHookType.WH_CBT,
                _cbtProc,
                IntPtr.Zero,
                NativeWindowApi.GetCurrentThreadId());
        }

        public void DetachCbtHook()
        {
            if (_cbtHook != IntPtr.Zero)
            {
                NativeWindowApi.UnhookWindowsHookEx(_cbtHook);
                _cbtHook = IntPtr.Zero;
            }
        }

        private static string EventName(uint evt) =>
            Enum.IsDefined(typeof(WinEventType), evt)
                ? Enum.GetName(typeof(WinEventType), evt)!
                : $"UNKNOWN_EVENT_{evt:X}";
    }
}
