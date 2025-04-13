using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Services.WindowStyle
{
    public class WindowStyleManager : IWindowStyleManager
    {
        private readonly IWindowStylePresetManager _presetManager;
        
        public WindowStyles GetStyle(IntPtr hWnd) =>
            (WindowStyles)(uint)NativeWindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_STYLE);

        public WindowExStyles GetExStyle(IntPtr hWnd) =>
            (WindowExStyles)(uint)NativeWindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE);

        public void SetStyle(IntPtr hWnd, WindowStyles style) =>
            NativeWindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_STYLE, (int)style);

        public void SetExStyle(IntPtr hWnd, WindowExStyles exStyle) =>
            NativeWindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE, (int)exStyle);

        public void ApplyStyleChanges(IntPtr hWnd)
        {
            NativeWindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                0, 0, 0, 0,
                (uint)(
                    SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_NOMOVE |
                    SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_FRAMECHANGED));
        }

        /// <summary>
        /// 应用样式预设（支持自定义键）
        /// </summary>
        public void ApplyPreset(IntPtr hWnd, string presetKey)
        {
            if (!_presetManager.TryGetPreset(presetKey, out var config))
                throw new ArgumentException($"找不到窗口样式预设：{presetKey}");

            SetStyle(hWnd, config.Style);
            SetExStyle(hWnd, config.ExStyle);
            ApplyStyleChanges(hWnd);

            if (config.Transparency is not null)
                SetWindowTransparency(hWnd, config.Transparency.Value);

            if (config.AlwaysTopmost)
                NativeWindowApi.SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                    (uint)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
        }

        private void SetWindowTransparency(IntPtr hWnd, double alpha)
        {
            if (alpha is < 0 or > 1) return;
            byte level = (byte)(alpha * 255);
            NativeWindowApi.SetLayeredWindowAttributes(hWnd, 0, level, 0x00000002); // LWA_ALPHA
        }
    }
}