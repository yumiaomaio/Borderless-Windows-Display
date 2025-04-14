using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public interface IDisplayConfigService
    {
        /// <summary>
        /// 设置指定显示器的分辨率 / 刷新率 / 位置 等
        /// </summary>
        bool ApplyDisplayConfiguration(DisplayConfigRequest request);
    }
}