using System.Drawing;

namespace BorderlessWindowApp.Services.WindowLayout
{
    public interface IWindowLayoutService
    {
        /// <summary>
        /// 设置窗口布局（位置 + 尺寸 + 策略）
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="size">目标大小（可以是客户区尺寸）</param>
        /// <param name="options">布局选项</param>
        void SetWindowLayout(IntPtr hWnd, Size size, WindowLayoutOptions options);

        /// <summary>
        /// 获取窗口边框外部矩形（含边框和标题栏）
        /// </summary>
        Rectangle GetWindowRect(IntPtr hWnd);

        /// <summary>
        /// 获取窗口客户区矩形
        /// </summary>
        Rectangle GetClientRect(IntPtr hWnd);
    }
}