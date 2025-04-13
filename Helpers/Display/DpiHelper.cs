using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Helpers.Display
{
    public static class DpiHelper
    {
        private static readonly uint[] DpiVals = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

        public class DpiScalingInfo
        {
            public uint Minimum { get; set; } = 100;
            public uint Maximum { get; set; } = 100;
            public uint Current { get; set; } = 100;
            public uint Recommended { get; set; } = 100;
            public bool IsInitialized { get; set; } = false;
        }

        public static DpiScalingInfo GetDpiScalingInfo(LUID adapterId, uint sourceId)
        {
            var header = new DISPLAYCONFIG_GET_DPI
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    adapterId = adapterId,
                    id = sourceId,
                    size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_DPI>(),
                    type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GET_DPI_SCALE
                }
            };

            int result = NativeDisplayApi.DisplayConfigGetDeviceInfo(ref header);
            if (result != 0) return new DpiScalingInfo();

            int offset = Math.Abs(header.minScaleRel);
            if (DpiVals.Length <= offset + header.maxScaleRel) return new DpiScalingInfo();

            return new DpiScalingInfo
            {
                Current = DpiVals[offset + header.curScaleRel],
                Recommended = DpiVals[offset],
                Maximum = DpiVals[offset + header.maxScaleRel],
                Minimum = 100,
                IsInitialized = true
            };
        }

        public static bool SetDpiScaling(LUID adapterId, uint sourceId, uint dpiPercent)
        {
            var dpiInfo = GetDpiScalingInfo(adapterId, sourceId);
            if (!dpiInfo.IsInitialized) return false;

            dpiPercent = Math.Clamp(dpiPercent, dpiInfo.Minimum, dpiInfo.Maximum);

            int idxTarget = Array.IndexOf(DpiVals, dpiPercent);
            int idxRecommended = Array.IndexOf(DpiVals, dpiInfo.Recommended);
            if (idxTarget < 0 || idxRecommended < 0) return false;

            int relative = idxTarget - idxRecommended;

            var setHeader = new DISPLAYCONFIG_SET_DPI
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    adapterId = adapterId,
                    id = sourceId,
                    size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_DPI>(),
                    type = DISPLAYCONFIG_DEVICE_INFO_TYPE.SET_DPI_SCALE
                },
                scaleRel = relative
            };

            return NativeDisplayApi.DisplayConfigSetDeviceInfo(ref setHeader) == 0;
        }
    }
}
