using System;
using System.Drawing;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Helpers.Window;
using BorderlessWindowApp.Interop.Delegates;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Services
{
    public class WindowMonitorService
    {
        private IntPtr _hookHandle = IntPtr.Zero;
        private WinEventDelegate _callback;
        private IntPtr _targetHwnd = IntPtr.Zero;

        public event Action<IntPtr, Rectangle> OnMovedOrResized;
        public event Action<IntPtr> OnShown;
        public event Action<IntPtr> OnHidden;
        public event Action<IntPtr> OnMinimized;
        public event Action<IntPtr> OnRestored;

        public void Start(IntPtr targetHwnd)
        {
            _targetHwnd = targetHwnd;

            _callback = (hWinEventHook, eventType, hwnd, idObject, idChild, dwThread, dwTime) =>
            {
                if (hwnd != _targetHwnd || idObject != 0)
                    return;

                switch ((WinEventType)eventType)
                {
                    case WinEventType.LocationChange:
                        var rect = WindowSizeHelper.GetWindowRect(hwnd);
                        OnMovedOrResized?.Invoke(hwnd, rect);
                        break;

                    case WinEventType.Show:
                        OnShown?.Invoke(hwnd);
                        break;

                    case WinEventType.Hide:
                        OnHidden?.Invoke(hwnd);
                        break;

                    case WinEventType.MinimizeStart:
                        OnMinimized?.Invoke(hwnd);
                        break;

                    case WinEventType.MinimizeEnd:
                        OnRestored?.Invoke(hwnd);
                        break;
                }
            };

            _hookHandle = Win32WindowApi.SetWinEventHook(
                (uint)WinEventType.MinimizeStart,
                (uint)WinEventType.LocationChange,
                IntPtr.Zero,
                _callback,
                0, 0,
                0x0000); // WINEVENT_OUTOFCONTEXT
        }

        public void Stop()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                Win32WindowApi.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }
}
