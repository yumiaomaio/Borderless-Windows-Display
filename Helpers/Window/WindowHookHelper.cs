using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Delegates;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class WindowHookHelper
    {
        // WinEvent hook
        private static WinEventDelegate _eventCallback;
        private static readonly List<IntPtr> _eventHooks = new();

        // CBT hook
        private static HookProc _cbtProc;
        private static IntPtr _cbtHook = IntPtr.Zero;
        private static IntPtr _targetHwnd = IntPtr.Zero;

        public static void StartWinEventHook(IntPtr targetHwnd, Action<string> onEvent)
        {
            _eventCallback = (hWinEventHook, eventType, hwnd, idObject, idChild, threadId, timestamp) =>
            {
                if (hwnd == targetHwnd)
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
                IntPtr hook = Win32WindowApi.SetWinEventHook((uint)evt, (uint)evt, IntPtr.Zero, _eventCallback, 0, 0, 0);
                if (hook != IntPtr.Zero)
                    _eventHooks.Add(hook);
            }
        }

        public static void StopWinEventHook()
        {
            foreach (var hook in _eventHooks)
            {
                Win32WindowApi.UnhookWinEvent(hook);
            }
            _eventHooks.Clear();
        }

        public static void StartCbtHook(IntPtr targetHwnd, Action<string> onIntercept = null)
        {
            _targetHwnd = targetHwnd;

            _cbtProc = (nCode, wParam, lParam) =>
            {
                CBTHookCode code = (CBTHookCode)nCode;
                if (code == CBTHookCode.HCBT_MINMAX && wParam == _targetHwnd)
                {
                    if (wParam == targetHwnd)
                    {
                        onIntercept?.Invoke($"[CBT_BLOCK] 阻止窗口最小化：{wParam}");
                        return 1; // 阻止
                    }
                }

                return Win32WindowApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            };

            _cbtHook = Win32WindowApi.SetWindowsHookEx((int)WindowsHookType.WH_CBT, _cbtProc, IntPtr.Zero, Win32WindowApi.GetCurrentThreadId());
        }

        public static void StopCbtHook()
        {
            if (_cbtHook != IntPtr.Zero)
            {
                Win32WindowApi.UnhookWindowsHookEx(_cbtHook);
                _cbtHook = IntPtr.Zero;
            }
        }

        private static string EventName(uint evt) =>
            Enum.IsDefined(typeof(WinEventType), evt)
                ? Enum.GetName(typeof(WinEventType), evt)
                : $"UNKNOWN_EVENT_{evt:X}";
    }
}
