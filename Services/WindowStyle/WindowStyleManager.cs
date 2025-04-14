using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services.WindowStyle
{
    public class WindowStyleManager : IWindowStyleManager
    {
        private readonly IWindowStylePresetManager _presetManager;
        private readonly ILogger<WindowStyleManager> _logger;

        public WindowStyleManager(IWindowStylePresetManager presetManager, ILogger<WindowStyleManager> logger)
        {
            _presetManager = presetManager;
            _logger = logger;
        }

        /// <summary>
        /// 获取窗口的标准样式（Style）
        /// </summary>
        public WindowStyles GetStyle(IntPtr hWnd)
        {
            var style = (WindowStyles)(uint)NativeWindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_STYLE);
            _logger.LogDebug("GetStyle: hWnd={Handle}, style={Style}", hWnd, style);
            return style;
        }

        /// <summary>
        /// 获取窗口的扩展样式（ExStyle）
        /// </summary>
        public WindowExStyles GetExStyle(IntPtr hWnd)
        {
            var exStyle = (WindowExStyles)(uint)NativeWindowApi.GetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE);
            _logger.LogDebug("GetExStyle: hWnd={Handle}, exStyle={ExStyle}", hWnd, exStyle);
            return exStyle;
        }

        /// <summary>
        /// 设置窗口的标准样式（Style）
        /// </summary>
        public void SetStyle(IntPtr hWnd, WindowStyles style)
        {
            NativeWindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_STYLE, (int)style);
            _logger.LogInformation("SetStyle: hWnd={Handle}, style={Style}", hWnd, style);
        }

        /// <summary>
        /// 设置窗口的扩展样式（ExStyle）
        /// </summary>
        public void SetExStyle(IntPtr hWnd, WindowExStyles exStyle)
        {
            NativeWindowApi.SetWindowLong(hWnd, WindowLongIndex.GWL_EXSTYLE, (int)exStyle);
            _logger.LogInformation("SetExStyle: hWnd={Handle}, exStyle={ExStyle}", hWnd, exStyle);
        }

        /// <summary>
        /// 强制刷新窗口以应用样式变化（需调用该方法使设置生效）
        /// </summary>
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

            _logger.LogInformation("Applied style changes to hWnd={Handle}", hWnd);
        }

        /// <summary>
        /// 应用指定键的样式预设，包括 Style、ExStyle、透明度和置顶标记
        /// </summary>
        public void ApplyPreset(IntPtr hWnd, string presetKey)
        {
            if (!_presetManager.TryGetPreset(presetKey, out var config))
            {
                _logger.LogError("Preset not found: {Key}", presetKey);
                throw new ArgumentException($"找不到窗口样式预设：{presetKey}");
            }

            _logger.LogInformation("Applying preset '{Key}' to hWnd={Handle}", presetKey, hWnd);

            SetStyle(hWnd, config.Style);
            SetExStyle(hWnd, config.ExStyle);
            ApplyStyleChanges(hWnd);

            if (config.Transparency is not null)
            {
                SetWindowTransparency(hWnd, config.Transparency.Value);
                _logger.LogDebug("Set transparency to {Alpha} for hWnd={Handle}", config.Transparency, hWnd);
            }

            if (config.AlwaysTopmost)
            {
                NativeWindowApi.SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                    (uint)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
                _logger.LogDebug("Window set to topmost for hWnd={Handle}", hWnd);
            }
        }

        /// <summary>
        /// 设置窗口的透明度（alpha：0.0 - 1.0）
        /// </summary>
        private void SetWindowTransparency(IntPtr hWnd, double alpha)
        {
            if (alpha is < 0 or > 1)
            {
                _logger.LogWarning("Invalid transparency value: {Alpha}", alpha);
                return;
            }

            byte level = (byte)(alpha * 255);
            NativeWindowApi.SetLayeredWindowAttributes(hWnd, 0, level, 0x00000002); // LWA_ALPHA
        }
    }
}
