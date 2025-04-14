using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    /// <summary>
    /// 提供 DPI 缩放查询与设置服务。
    /// </summary>
    public class DisplayScaleService : IDisplayScaleService
    {
        private static readonly uint[] DpiVals = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };
        private readonly ILogger<DisplayScaleService> _logger;

        public DisplayScaleService(ILogger<DisplayScaleService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取指定适配器和源的 DPI 缩放信息。
        /// </summary>
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
            if (result != 0)
            {
                _logger.LogWarning("Failed to get DPI info for adapter {Adapter}, source {Source}, result={Result}", adapterId, sourceId, result);
                return new DpiScalingInfo();
            }

            int offset = Math.Abs(header.minScaleRel);
            if (DpiVals.Length <= offset + header.maxScaleRel)
            {
                _logger.LogWarning("DPI offset range out of bounds. Offset={Offset}, MaxRel={MaxRel}", offset, header.maxScaleRel);
                return new DpiScalingInfo();
            }

            var info = new DpiScalingInfo
            {
                Current = DpiVals[offset + header.curScaleRel],
                Recommended = DpiVals[offset],
                Maximum = DpiVals[offset + header.maxScaleRel],
                Minimum = 100,
                IsInitialized = true
            };

            _logger.LogInformation("DPI info: Current={Current}%, Recommended={Recommended}%, Max={Max}%", info.Current, info.Recommended, info.Maximum);
            return info;
        }

        /// <summary>
        /// 设置 DPI 缩放百分比。
        /// </summary>
        public bool SetScaling(LUID adapterId, uint sourceId, uint dpiPercent)
        {
            var info = GetScalingInfo(adapterId, sourceId);
            if (!info.IsInitialized)
            {
                _logger.LogWarning("Cannot set DPI. Scaling info not available.");
                return false;
            }

            dpiPercent = Math.Clamp(dpiPercent, info.Minimum, info.Maximum);

            int idxTarget = Array.IndexOf(DpiVals, dpiPercent);
            int idxRecommended = Array.IndexOf(DpiVals, info.Recommended);
            if (idxTarget < 0 || idxRecommended < 0)
            {
                _logger.LogWarning("Invalid DPI value. Requested={Requested}, Recommended={Recommended}", dpiPercent, info.Recommended);
                return false;
            }

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

            bool success = NativeDisplayApi.DisplayConfigSetDeviceInfo(ref setHeader) == 0;
            if (!success)
                _logger.LogError("Failed to apply DPI setting: {Dpi}%", dpiPercent);
            else
                _logger.LogInformation("DPI setting applied: {Dpi}%", dpiPercent);

            return success;
        }
    }
}
