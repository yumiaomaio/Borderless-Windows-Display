using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Constants;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display.Models;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services.Display.implement
{
    /// <summary>
    /// 提供显示设备信息服务，包括设备枚举、支持模式查询和当前模式获取。
    /// </summary>
    public class DisplayInfoService : IDisplayInfoService
    {
        private readonly ILogger<DisplayInfoService> _logger;

        public DisplayInfoService(ILogger<DisplayInfoService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                //_logger.LogDebug("Mode found for {Device}: {Width}x{Height} @ {Hz}Hz", deviceName, mode.Width,mode.Height, mode.RefreshRate);
            }

            _logger.LogInformation("Total display modes found for {Device}: {Count}", deviceName, modes.Count);
            return modes.DistinctBy(m => (m.Width, m.Height, m.RefreshRate)).ToList();
        }

        /// <summary>
        /// 获取显示设备当前正在使用的显示模式。
        /// </summary>
        public DisplayModeInfo? GetCurrentMode(string deviceName)
        {
            DEVMODE devMode = default;
            // Initialize dmSize using Marshal.SizeOf with *your* DEVMODE struct
            devMode.dmSize = (ushort)Marshal.SizeOf<DEVMODE>();

            if (NativeDisplayApi.EnumDisplaySettingsEx(deviceName, IModeNum.ENUM_CURRENT_SETTINGS, ref devMode, 0))
            {
                // Map the uint dmDisplayOrientation to your DisplayOrientation enum
                DisplayOrientation orientation = devMode.dmDisplayOrientation switch
                {
                    DisplayOrientationConstants.DMDO_90 => DisplayOrientation.Portrait,
                    DisplayOrientationConstants.DMDO_180 => DisplayOrientation.LandscapeFlipped,
                    DisplayOrientationConstants.DMDO_270 => DisplayOrientation.PortraitFlipped,
                    _ => DisplayOrientation.Landscape, // Default or DMDO_DEFAULT
                };

                return new DisplayModeInfo
                {
                    // Cast uint fields from DEVMODE to int for DisplayModeInfo
                    Width = (int)devMode.dmPelsWidth,
                    Height = (int)devMode.dmPelsHeight,
                    RefreshRate = (int)devMode.dmDisplayFrequency,
                    Orientation = orientation
                };
            }
            else
            {
                _logger.LogWarning("Error getting current settings for device {DeviceName}. Error code: {ErrorCode}", deviceName, Marshal.GetLastWin32Error());
                return null;
            }
        }

        #endregion
        
        
        #region DisplayConfig
        private (string? DeviceName, string? DeviceString, POINTL? Position,uint height,uint Width)? 
            GetSourceInfo_MapDeviceName(LUID adapterId, uint sourceId)
        {
            _logger.LogInformation("[GetSourceInfo_MapDeviceName] Starting GDI mapping for adapterId={AdapterId}, sourceId={SourceId}", adapterId, sourceId);

            // 先获取目标 source 的位置（position）
            uint pathCount = 0, modeCount = 0;
            NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount, ref modeCount);
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);

            POINTL? sourcePos = null;
            uint Height = 0;
            uint Width = 0;
            foreach (var mode in modes)
            {
                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.SOURCE &&
                    mode.adapterId.HighPart == adapterId.HighPart &&
                    mode.adapterId.LowPart == adapterId.LowPart &&
                    mode.id == sourceId)
                {
                    sourcePos = mode.sourceMode.position;
                    Height = mode.sourceMode.height;
                    Width = mode.sourceMode.width;
                    break;
                }
            }

            if (sourcePos == null)
            {
                _logger.LogWarning("[GetSourceInfo_MapDeviceName] No matching position found for adapterId={AdapterId}, sourceId={SourceId}", adapterId, sourceId);
                return null;
            }

            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);
            uint devNum = 0;

            string? mappedDeviceName = null;
            string? mappedDeviceString = null;
            while (NativeDisplayApi.EnumDisplayDevices(null, devNum++, ref d, 0))
            {
                // Check if the device is part of the desktop (active monitor)
                if ((d.StateFlags & 0x00000001) == 0) continue; // DISPLAY_DEVICE_ATTACHED_TO_DESKTOP

                DEVMODE devMode = new() { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
                // Get the current settings for this display device
                if (NativeDisplayApi.EnumDisplaySettings(d.DeviceName, -1 /* ENUM_CURRENT_SETTINGS */, ref devMode))
                {
                    // Compare the GDI position with the position found via QueryDisplayConfig
                    if (devMode.dmPositionX == sourcePos.Value.x && devMode.dmPositionY == sourcePos.Value.y)
                    {
                        // Found a match
                        mappedDeviceName = d.DeviceName;
                        mappedDeviceString = d.DeviceString;
                        _logger.LogInformation("[GetSourceInfo_MapDeviceName] Found matching GDI device: {DeviceName} at position ({X}, {Y})", d.DeviceName, devMode.dmPositionX, devMode.dmPositionY);
                        break; // Stop searching once a match is found
                    }
                }

                // Reset cb size for the next call, as EnumDisplayDevices might modify it.
                d.cb = Marshal.SizeOf(d);
            }

            if (mappedDeviceName == null)
            {
                _logger.LogWarning("[GetSourceInfo_MapDeviceName] No matching GDI device found for adapterId={AdapterId}, sourceId={SourceId}", adapterId, sourceId);
            }

            // Return the found GDI names (or null if not found) AND the position from QueryDisplayConfig
            return (mappedDeviceName, mappedDeviceString, sourcePos, Height, Width);
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
                var mapping = GetSourceInfo_MapDeviceName(adapterId, sourceId);
                if (mapping.HasValue)
                {
                    display.DeviceName = mapping.Value.DeviceName;
                    display.DeviceString = mapping.Value.DeviceString;
                    if (mapping.Value.Position.HasValue)
                    {
                        display.PositionX = mapping.Value.Position.Value.x;
                        display.PositionY = mapping.Value.Position.Value.y;
                    }
                    display.Height = mapping.Value.height;
                    display.Width = mapping.Value.Width;
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
        
    }
}