using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Models;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class WindowStyleHelper
    {
        public static WindowStyles GetStyle(IntPtr hWnd) =>
            (WindowStyles)(uint)Win32WindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_STYLE);

        public static WindowExStyles GetExStyle(IntPtr hWnd) =>
            (WindowExStyles)(uint)Win32WindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE);

        public static void SetStyle(IntPtr hWnd, WindowStyles style)
        {
            Win32WindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_STYLE, (int)style);
        }

        public static void SetExStyle(IntPtr hWnd, WindowExStyles exStyle)
        {
            Win32WindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE, (int)exStyle);
        }

        /// <summary>
        /// 应用指定窗口样式预设（如标准、无边框等）
        /// </summary>
        public enum WindowStylePreset
        {
            Standard, // 普通窗口（有标题栏、系统按钮、可缩放）
            Borderless, // 无边框纯净窗口
            ToolWindow, // 工具窗口，无任务栏按钮
            Overlay, // 透明、置顶、穿透窗口
            Dialog, // 对话框风格（无最大最小化按钮）
            FullScreen, // 真全屏（无边框 + 屏幕填充）
            Popup, // 弹出框（无边框 + 无任务栏）
            DebugOverlay // 半透明可点击窗口，用于调试浮层
        }

        public static void ApplyPreset(IntPtr hWnd, WindowStylePreset preset)
        {
            if (!PresetConfigs.TryGetValue(preset, out var config))
                throw new ArgumentException($"未知窗口样式预设：{preset}");

            SetStyle(hWnd, config.Style);
            SetExStyle(hWnd, config.ExStyle);
            ApplyStyleChanges(hWnd);

            if (config.Transparency is not null)
                LayeredWindowHelper.SetTransparency(hWnd, config.Transparency.Value);

            if (config.AlwaysTopmost)
                Win32WindowApi.SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                    (uint)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
        }

        public static void ApplyStyleChanges(IntPtr hWnd)
        {
            Win32WindowApi.SetWindowPos(
                hWnd,
                IntPtr.Zero,
                0, 0, 0, 0,
                (uint)(
                    SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_NOMOVE |
                    SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_FRAMECHANGED));
        }

        private static readonly Dictionary<WindowStylePreset, WindowStylePresetConfig> PresetConfigs = new()
        {
            [WindowStylePreset.Standard] = new(
                Style: WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_CLIENTEDGE | WindowExStyles.WS_EX_STATICEDGE,
                Description: "标准窗口：有边框、标题栏、最小化最大化按钮",
                AllowResize: true
            ),

            [WindowStylePreset.Borderless] = new(
                Style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.None,
                Description: "无边框窗口：适合自绘界面、沉浸式体验",
                AllowResize: false
            ),

            [WindowStylePreset.ToolWindow] = new(
                Style: WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_TOOLWINDOW | WindowExStyles.WS_EX_TOPMOST,
                Description: "工具窗口：紧凑、无任务栏图标，自动置顶",
                AlwaysTopmost: true,
                AllowResize: false
            ),

            [WindowStylePreset.Overlay] = new(
                Style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TRANSPARENT | WindowExStyles.WS_EX_TOPMOST,
                Description: "HUD 叠加层：透明、穿透、置顶显示",
                AlwaysTopmost: true,
                AllowResize: false,
                Transparency: 0.6
            ),

            [WindowStylePreset.Dialog] = new(
                Style: WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_DLGMODALFRAME,
                Description: "对话框风格：不可最大/最小化，简洁标题栏",
                AllowResize: false
            ),

            [WindowStylePreset.FullScreen] = new(
                Style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_TOPMOST,
                Description: "全屏窗口：适合游戏或沉浸模式",
                AlwaysTopmost: true,
                AllowResize: false
            ),

            [WindowStylePreset.Popup] = new(
                Style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_TOOLWINDOW,
                Description: "弹出窗口：适合菜单、提示气泡",
                AllowResize: false
            ),

            [WindowStylePreset.DebugOverlay] = new(
                Style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                ExStyle: WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TOPMOST,
                Description: "调试浮层：透明、可点击、用于状态展示",
                AlwaysTopmost: true,
                Transparency: 0.8
            )
        };
    }
}