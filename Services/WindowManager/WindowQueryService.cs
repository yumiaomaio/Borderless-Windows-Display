using System.Text;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Services.Window;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services
{
    public class WindowQueryService : IWindowQueryService
    {
        private readonly ILogger<WindowQueryService> _logger;

        public WindowQueryService(ILogger<WindowQueryService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 查找标题中包含指定关键词的可见窗口句柄
        /// </summary>
        public IntPtr? FindByTitle(string title)
        {
            IntPtr result = IntPtr.Zero;

            // 枚举所有顶层窗口
            NativeWindowApi.EnumWindows((hWnd, lParam) =>
            {
                // 跳过不可见窗口
                if (!NativeWindowApi.IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(256);
                NativeWindowApi.GetWindowText(hWnd, sb, sb.Capacity);
                string windowTitle = sb.ToString();

                // 模糊匹配窗口标题
                if (windowTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("匹配窗口：{Title} ({Handle})", windowTitle, hWnd);
                    result = hWnd;
                    return false; // 找到后中断枚举
                }

                return true;
            }, IntPtr.Zero);

            if (result == IntPtr.Zero)
            {
                _logger.LogWarning("未找到包含关键字 \"{Keyword}\" 的窗口。", title);
                return null;
            }

            return result;
        }

        /// <summary>
        /// 获取当前所有可见窗口的标题
        /// </summary>
        public List<string> GetAllVisibleWindowTitles()
        {
            List<string> titles = new();

            // 遍历所有可见窗口并收集标题
            NativeWindowApi.EnumWindows((hWnd, lParam) =>
            {
                if (!NativeWindowApi.IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(256);
                NativeWindowApi.GetWindowText(hWnd, sb, sb.Capacity);

                string title = sb.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    titles.Add(title);
                    _logger.LogDebug("检测到窗口：{Title}", title);
                }

                return true;
            }, IntPtr.Zero);

            _logger.LogInformation("当前可见窗口数量：{Count}", titles.Count);
            return titles;
        }
    }
}
