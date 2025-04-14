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
    /// 提供显示器配置的应用服务，例如分辨率、刷新率、色深与位置等。
    /// </summary>
    public class DisplayConfigService : IDisplayConfigService
    {
        private readonly ILogger<DisplayConfigService> _logger;

        public DisplayConfigService(ILogger<DisplayConfigService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 应用显示器的配置（分辨率、位置、刷新率等）。
        /// </summary>
        /// <param name="request">配置参数</param>
        /// <returns>操作是否成功</returns>
        public bool ApplyDisplayConfiguration(DisplayConfigRequest request)
        {
            try
            {
                var devmode = BuildDevMode(request);
                return ApplyDevMode(request.DeviceName, devmode, request.SetAsPrimary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyDisplayConfiguration failed for device: {Device}", request.DeviceName);
                return false;
            }
        }

        private DEVMODE BuildDevMode(DisplayConfigRequest request)
        {
            var devmode = new DEVMODE
            {
                dmSize = (ushort)Marshal.SizeOf<DEVMODE>(),
                dmPelsWidth = (uint)request.Width,
                dmPelsHeight = (uint)request.Height,
                dmDisplayFrequency = (uint)request.RefreshRate,
                dmBitsPerPel = (uint)request.BitDepth,
                dmFields = (uint)(
                    DisplayDeviceModeFields.PelsWidth |
                    DisplayDeviceModeFields.PelsHeight |
                    DisplayDeviceModeFields.DisplayFrequency |
                    DisplayDeviceModeFields.BitsPerPel)
            };

            if (request.PositionX.HasValue && request.PositionY.HasValue)
            {
                devmode.dmPositionX = request.PositionX.Value;
                devmode.dmPositionY = request.PositionY.Value;
                devmode.dmFields |= (uint)DisplayDeviceModeFields.Position;
            }

            return devmode;
        }

        private bool ApplyDevMode(string deviceName, DEVMODE devmode, bool setAsPrimary)
        {
            const int CDS_UPDATEREGISTRY = 0x00000001;
            const int CDS_GLOBAL = 0x00000008;
            const int CDS_SET_PRIMARY = 0x00000010;

            int flags = CDS_UPDATEREGISTRY | CDS_GLOBAL;
            if (setAsPrimary)
                flags |= CDS_SET_PRIMARY;

            int result = NativeDisplayApi.ChangeDisplaySettingsEx(
                deviceName,
                ref devmode,
                IntPtr.Zero,
                flags,
                IntPtr.Zero);

            if (result != DisplayConstants.DISP_CHANGE_SUCCESSFUL)
            {
                _logger.LogWarning("ChangeDisplaySettingsEx failed with code {Code} for device {Device}", result, deviceName);
                return false;
            }

            _logger.LogInformation("Display configuration applied successfully to device: {Device}", deviceName);
            return true;
        }
    }
}
