using System;

namespace BorderlessWindowApp.Services.Window
{
    public interface IWindowManagerService
    {
        /// <summary>
        /// 初始化窗口：应用样式、定位、钩子
        /// </summary>
        void InitWindow(string titleKeyword, string stylePreset, int width, int height);

        /// <summary>
        /// 尝试激活某个窗口，如果找不到则不操作
        /// </summary>
        void FocusWindow(string titleKeyword);

        /// <summary>
        /// 应用样式预设（透明、置顶、样式）
        /// </summary>
        void ApplyStyle(string titleKeyword, string presetKey);

        /// <summary>
        /// 停止钩子监听（窗口退出时）
        /// </summary>
        void Cleanup();
    }
}