using System;
using System.Collections.Generic;
using System.Drawing;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Helpers.Window;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Models;

namespace BorderlessWindowApp.Services
{
    public class WindowManagerService
    {
        private readonly Dictionary<IntPtr, WindowStateSnapshot> _snapshots = new();

        public IntPtr FindWindow(string title) =>
            WindowFinder.FindByTitle(title);
        
        public List<string> GetAllVisibleWindowTitles() => WindowFinder.GetAllVisibleTitles();


        public void Snapshot(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            var bounds = WindowSizeHelper.GetWindowRect(hWnd);
            var style = WindowStyleHelper.GetStyle(hWnd);
            var exStyle = WindowStyleHelper.GetExStyle(hWnd);
            bool visible = Win32WindowApi.IsWindowVisible(hWnd);

            _snapshots[hWnd] = new WindowStateSnapshot
            {
                Handle = hWnd,
                Bounds = bounds,
                Style = style,
                ExStyle = exStyle,
                IsVisible = visible
            };
        }

        public void RestoreSnapshot(IntPtr hWnd)
        {
            if (!_snapshots.TryGetValue(hWnd, out var snap)) return;

            WindowStyleHelper.SetStyle(hWnd, snap.Style);
            WindowStyleHelper.SetExStyle(hWnd, snap.ExStyle);

            Win32WindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                snap.Bounds.X,
                snap.Bounds.Y,
                snap.Bounds.Width,
                snap.Bounds.Height,
                (uint)(SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_FRAMECHANGED));

            Win32WindowApi.ShowWindow(hWnd, snap.IsVisible ? (int)ShowWindowCommand.Show : (int)ShowWindowCommand.Hide);
        }

        public void ApplyStyle(IntPtr hWnd, WindowStyleHelper.WindowStylePreset preset)
        {
            WindowStyleHelper.ApplyPreset(hWnd, preset);
        }

        public void CenterWindow(IntPtr hWnd)
        {
            WindowPositionHelper.CenterWindowToScreen(hWnd);
        }

        public void SetClientSize(IntPtr hWnd, int width, int height)
        {
            WindowSizeHelper.SetClientSize(hWnd, width, height);
        }

        public void MoveWindow(IntPtr hWnd, int x, int y)
        {
            Win32WindowApi.SetWindowPos(
                hWnd, IntPtr.Zero, x, y, 0, 0,
                (uint)(SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER));
        }

        public void SetTopmost(IntPtr hWnd, bool topmost)
        {
            Win32WindowApi.SetWindowPos(
                hWnd,
                topmost ? new IntPtr(-1) : new IntPtr(-2), // HWND_TOPMOST or NOTOPMOST
                0, 0, 0, 0,
                (uint)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
        }

        public void Show(IntPtr hWnd)
        {
            Win32WindowApi.ShowWindow(hWnd, (int)ShowWindowCommand.Show);
        }

        public void Hide(IntPtr hWnd)
        {
            Win32WindowApi.ShowWindow(hWnd, (int)ShowWindowCommand.Hide);
        }

        public void Restore(IntPtr hWnd)
        {
            if (Win32WindowApi.IsIconic(hWnd))
                Win32WindowApi.ShowWindow(hWnd, (int)ShowWindowCommand.Restore);
        }
    }
    
}
