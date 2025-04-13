using System.Text;
using BorderlessWindowApp.Interop;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class WindowFinder
    {
        /// <summary>
        /// 查找包含指定标题的窗口句柄
        /// </summary>
        public static IntPtr FindByTitle(string title)
        {
            IntPtr result = IntPtr.Zero;

            Win32WindowApi.EnumWindows((hWnd, lParam) =>
            {
                if (!Win32WindowApi.IsWindowVisible(hWnd)) return true;

                var sb = new StringBuilder(256);
                Win32WindowApi.GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    result = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        /// <summary>
        /// 获取所有可见窗口的标题
        /// </summary>
        public static List<string> GetAllVisibleTitles()
        {
            List<string> titles = new();

            Win32WindowApi.EnumWindows((hWnd, lParam) =>
            {
                if (!Win32WindowApi.IsWindowVisible(hWnd))
                    return true;

                var sb = new System.Text.StringBuilder(256);
                Win32WindowApi.GetWindowText(hWnd, sb, sb.Capacity);

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
