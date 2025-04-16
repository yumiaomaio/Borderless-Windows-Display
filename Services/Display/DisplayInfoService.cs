using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Constants;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    /// <summary>
    /// 提供显示设备信息服务，包括设备枚举、支持模式查询和当前模式获取。
    /// </summary>
    public class DisplayInfoService : IDisplayInfoService
    {
        private readonly ILogger<DisplayInfoService> _logger;

        public DisplayInfoService(ILogger<DisplayInfoService> logger)
        {
            _logger = logger;
        }

        #region EnumDisplay

        /// <summary>
        /// 获取所有激活的显示设备名称。
        /// </summary>
        public IEnumerable<string> GetAllDeviceNames()
        {
            List<string> names = new();
            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);

            uint devNum = 0;
            while (NativeDisplayApi.EnumDisplayDevices(null, devNum++, ref d, 0))
            {
                if ((d.StateFlags & 0x00000001) != 0) // DISPLAY_DEVICE_ACTIVE
                {
                    names.Add(d.DeviceName); // "\\.\DISPLAY1"
                    _logger.LogInformation("Found active display device: {DeviceName} {DeviceString}", d.DeviceName,d.DeviceString);
                }

                d.cb = Marshal.SizeOf(d);
            }

            _logger.LogInformation("Total active display devices found: {Count}", names.Count);
            return names;
        }

        /// <summary>
        /// 获取指定显示设备支持的所有显示模式（分辨率 + 刷新率）。
        /// </summary>
        public IEnumerable<DisplayModeInfo> GetSupportedModes(string deviceName)
        {
            List<DisplayModeInfo> modes = new();
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };

            int i = 0;
            while (NativeDisplayApi.EnumDisplaySettings(deviceName, i++, ref devMode))
            {
                var mode = new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                };
                modes.Add(mode);
                _logger.LogDebug("Mode found for {Device}: {Width}x{Height} @ {Hz}Hz", deviceName, mode.Width,
                    mode.Height, mode.RefreshRate);
            }

            _logger.LogInformation("Total display modes found for {Device}: {Count}", deviceName, modes.Count);
            return modes.DistinctBy(m => (m.Width, m.Height, m.RefreshRate)).ToList();
        }

        /// <summary>
        /// 获取显示设备当前正在使用的显示模式。
        /// </summary>
        public DisplayModeInfo? GetCurrentMode(string deviceName)
        {
            DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)) };
            const int ENUM_CURRENT_SETTINGS = -1;

            if (NativeDisplayApi.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                var mode = new DisplayModeInfo
                {
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency
                };
                _logger.LogInformation("Current mode for {Device}: {Width}x{Height} @ {Hz}Hz", deviceName, mode.Width,
                    mode.Height, mode.RefreshRate);
                return mode;
            }

            _logger.LogWarning("Failed to get current mode for device: {Device}", deviceName);
            return null;
        }

        #endregion
        
        
        #region DisplayConfig
        private (string? DeviceName, string? DeviceString)? TryMapSourceIdToDeviceName(LUID adapterId, uint sourceId)
        {
            // 先获取目标 source 的位置（position）
            uint pathCount = 0, modeCount = 0;
            NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount, ref modeCount);
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);

            POINTL? sourcePos = null;

            foreach (var mode in modes)
            {
                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.SOURCE &&
                    mode.adapterId.HighPart == adapterId.HighPart &&
                    mode.adapterId.LowPart == adapterId.LowPart &&
                    mode.id == sourceId)
                {
                    sourcePos = mode.sourceMode.position;
                    break;
                }
            }

            if (sourcePos == null) return null;

            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);
            uint devNum = 0;

            while (NativeDisplayApi.EnumDisplayDevices(null, devNum++, ref d, 0))
            {
                if ((d.StateFlags & 0x00000001) == 0) continue;

                DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
                if (NativeDisplayApi.EnumDisplaySettings(d.DeviceName, -1, ref devMode))
                {
                    if (devMode.dmPositionX == sourcePos.Value.x && devMode.dmPositionY == sourcePos.Value.y)
                    {
                        return (d.DeviceName, d.DeviceString);
                    }
                }

                d.cb = Marshal.SizeOf(d);
            }

            return null;
        }

        private DisplayTargetDetails? GetDisplayTargetInfo(LUID adapterId, uint targetId)
        {
            var query = new DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    adapterId = adapterId,
                    id = targetId,
                    size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME
                }
            };

            int status = NativeDisplayApi.DisplayConfigGetDeviceInfo(ref query);
            if (status != 0)
            {
                _logger.LogWarning($"[DisplayInfoService] GetDeviceInfo failed. AdapterId={adapterId.LowPart}/{adapterId.HighPart}, TargetId={targetId}, Status={status}");
                return null;
            }

            return new DisplayTargetDetails
            {
                FriendlyName = query.monitorFriendlyDeviceName,
                DevicePath = query.monitorDevicePath,
                OutputTechnology = query.outputTechnology,
                EdidManufactureId = query.edidManufactureId,
                EdidProductCodeId = query.edidProductCodeId,
                ConnectorInstance = query.connectorInstance
            };
        }
        
        public List<DisplayDeviceInfo> GetAllDisplayDevices()
        {
            var results = new List<DisplayDeviceInfo>();

            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0) return results;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0) return results;

            foreach (var path in paths)
            {
                var adapterId = path.sourceInfo.adapterId;
                var sourceId = path.sourceInfo.id;
                var targetId = path.targetInfo.id;
                var available = path.targetInfo.targetAvailable;

                var display = new DisplayDeviceInfo
                {
                    AdapterId = adapterId,
                    SourceId = sourceId,
                    TargetId = targetId,
                    IsAvailable = available
                };

                // 获取 target 的详细信息
                var targetDetails = GetDisplayTargetInfo(adapterId, targetId);
                if (targetDetails != null)
                {
                    display.FriendlyName = targetDetails.FriendlyName;
                    display.DevicePath = targetDetails.DevicePath;
                    display.OutputTechnology = targetDetails.OutputTechnology;
                }

                // 尝试从 GDI 映射 deviceName 和 deviceString
                var mapping = TryMapSourceIdToDeviceName(adapterId, sourceId);
                if (mapping.HasValue)
                {
                    display.DeviceName = mapping.Value.DeviceName;
                    display.DeviceString = mapping.Value.DeviceString;
                }

                results.Add(display);
            }

            return results;
        }

        #endregion
        
        public void TestDisplayTargets()
        {

            GetAllDeviceNames();

        }

        #region api

        

        public const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;

        
        #endregion
    }
}