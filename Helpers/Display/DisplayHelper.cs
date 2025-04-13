using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Helpers.Display;

public static class DisplayHelper
{
    public static bool QueryDisplayPathsAndModes(out List<DISPLAYCONFIG_PATH_INFO> paths, out List<DISPLAYCONFIG_MODE_INFO> modes, uint flags = 0x00000001)
    {
        paths = new();
        modes = new();

        if (DisplayConfigApi.GetDisplayConfigBufferSizes(flags, out uint numPaths, out uint numModes) != 0)
            return false;

        var pathArray = new DISPLAYCONFIG_PATH_INFO[numPaths];
        var modeArray = new DISPLAYCONFIG_MODE_INFO[numModes];

        if (DisplayConfigApi.QueryDisplayConfig(flags, ref numPaths, pathArray, ref numModes, modeArray, IntPtr.Zero) != 0)
            return false;

        paths.AddRange(pathArray);
        modes.AddRange(modeArray);
        return true;
    }
}