using BorderlessWindowApp.Models;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public interface IDisplayScaleService
    {
        DpiScalingInfo GetScalingInfo(LUID adapterId, uint sourceId);
        bool SetScaling(LUID adapterId, uint sourceId, uint dpiPercent);
    }
}