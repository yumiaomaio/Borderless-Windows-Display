using System;
using System.Linq;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Models;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public class DisplayScaleService : IDisplayScaleService
    {
        private static readonly uint[] DpiVals = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

        public DpiScalingInfo GetScalingInfo(LUID adapterId, uint sourceId)
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

        public bool SetScaling(LUID adapterId, uint sourceId, uint dpiPercent)
        {
            var info = GetScalingInfo(adapterId, sourceId);
            if (!info.IsInitialized) return false;

            dpiPercent = Math.Clamp(dpiPercent, info.Minimum, info.Maximum);

            int idxTarget = Array.IndexOf(DpiVals, dpiPercent);
            int idxRecommended = Array.IndexOf(DpiVals, info.Recommended);
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
