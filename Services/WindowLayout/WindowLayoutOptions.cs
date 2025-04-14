using System.Drawing;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;

namespace BorderlessWindowApp.Services.WindowLayout
{
    public class WindowLayoutOptions
    {
        /// <summary>
        /// 是否将传入尺寸视为客户区尺寸（会自动通过 AdjustWindowRectEx 转换为窗口大小）
        /// </summary>
        public bool UseClientSize { get; set; } = true;

        /// <summary>
        /// 是否将窗口居中显示（忽略 Location）
        /// </summary>
        public bool CenterToScreen { get; set; } = false;

        /// <summary>
        /// 手动设置窗口位置（优先级低于 CenterToScreen）
        /// </summary>
        public Point? Location { get; set; }

        /// <summary>
        /// SetWindowPos 使用的标志位
        /// </summary>
        public SetWindowPosFlags Flags { get; set; } =
            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_FRAMECHANGED;
        
        /// <summary>控制定位使用哪个显示器</summary>
        public RelativeMonitor MonitorTarget { get; set; } = RelativeMonitor.Primary;

        /// <summary>如果 MonitorTarget 为 Specific，则用此 Index</summary>
        public int MonitorIndex { get; set; } = 0;
    }
}