using System;
using System.Collections.Generic;
using System.Text;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Services.Window;

namespace BorderlessWindowApp.Services
{
    public class WindowQueryService : IWindowQueryService
    {
        /// <summary>
        /// 查找标题中包含指定关键词的可见窗口句柄
        /// </summary>
        public IntPtr? FindByTitle(string title)
        {
            IntPtr result = IntPtr.Zero;

            NativeWindowApi.EnumWindows((hWnd, lParam) =>
            {
                if (!NativeWindowApi.IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(256);
                NativeWindowApi.GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    result = hWnd;
                    return false; // 中断枚举
                }

                return true;
            }, IntPtr.Zero);

            return result == IntPtr.Zero ? null : result;
        }

        /// <summary>
        /// 获取当前所有可见窗口的标题
        /// </summary>
        public List<string> GetAllVisibleWindowTitles()
        {
            List<string> titles = new();

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
                }

                return true;
            }, IntPtr.Zero);

            return titles;
        }
    }
}