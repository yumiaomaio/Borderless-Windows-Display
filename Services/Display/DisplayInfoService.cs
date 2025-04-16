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

        #region DISPLAY

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
                    _logger.LogDebug("Found active display device: {DeviceName}", d.DeviceName);
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

        public List<(LUID adapterId, uint sourceId, uint targetId, bool isAvailable)> GetAllDisplayTargets()
        {
            var list = new List<(LUID, uint, uint, bool)>();

            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0) return list;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount,
                paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0) return list;

            foreach (var path in paths)
            {
                var adapterId = path.targetInfo.adapterId;
                var targetId = path.targetInfo.id;
                var sourceId = path.sourceInfo.id;
                bool available = path.targetInfo.targetAvailable;

                list.Add((adapterId, sourceId, targetId, available));
            }

            return list;
        }

        public (string? devicePath, string? friendlyName)? GetDisplayTargetInfo(LUID adapterId, uint targetId)
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
                _logger.LogWarning(
                    $"[DisplayInfoService] GetDeviceInfo failed. AdapterId={adapterId.LowPart}/{adapterId.HighPart}, TargetId={targetId}, Status={status}");
                return null;
            }

            _logger.LogInformation($"  outputTechnology: {query.outputTechnology}");
            _logger.LogInformation(
                $"  edidManufactureId: {query.edidManufactureId} ({EdidVendorIdToString(query.edidManufactureId)})");
            _logger.LogInformation($"  edidProductCodeId: {query.edidProductCodeId}");
            _logger.LogInformation($"  connectorInstance: {query.connectorInstance}");
            _logger.LogInformation($"  monitorFriendlyDeviceName: {query.monitorFriendlyDeviceName}");
            _logger.LogInformation($"  monitorDevicePath: {query.monitorDevicePath}");


            return (query.monitorDevicePath, query.monitorFriendlyDeviceName);
        }

        private string EdidVendorIdToString(ushort id)
        {
            // EDID Vendor ID 是 3 字符编码，例如 'ACR' (Acer), 'DEL' (Dell)
            char c1 = (char)(((id >> 10) & 0x1F) + 64);
            char c2 = (char)(((id >> 5) & 0x1F) + 64);
            char c3 = (char)((id & 0x1F) + 64);
            return $"{c1}{c2}{c3}";
        }

        public void DumpQueryDisplayConfigInfo()
        {
            _logger.LogInformation("[Dump] Start dumping display configuration info");

            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0)
            {
                _logger.LogWarning($"[Dump] GetDisplayConfigBufferSizes failed. Status={result}");
                return;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount,
                paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0)
            {
                _logger.LogWarning($"[Dump] QueryDisplayConfig failed. Status={result}");
                return;
            }

            _logger.LogInformation($"[Dump] Paths: {pathCount}, Modes: {modeCount}");

            for (int i = 0; i < pathCount; i++)
            {
                var path = paths[i];
                var adapter = path.sourceInfo.adapterId;

                _logger.LogInformation($"[Path {i}]");
                _logger.LogInformation($"  AdapterId: {adapter.LowPart}/{adapter.HighPart}");
                _logger.LogInformation($"  SourceId: {path.sourceInfo.id}");
                _logger.LogInformation($"  TargetId: {path.targetInfo.id}");
                _logger.LogInformation($"  TargetAvailable: {path.targetInfo.targetAvailable}");
                _logger.LogInformation($"  Flags: {path.flags}");

                var rr = path.targetInfo.refreshRate;
                _logger.LogInformation($"  RefreshRate: {rr.Numerator}/{rr.Denominator}");

                _logger.LogInformation($"  OutputTechnology: {path.targetInfo.outputTechnology}");
                _logger.LogInformation($"  Scaling: {path.targetInfo.scaling}");
                _logger.LogInformation($"  Rotation: {path.targetInfo.rotation}");
                _logger.LogInformation($"  ScanlineOrdering: {path.targetInfo.scanLineOrdering}");
            }

            // Dump source/target mode details
            for (int i = 0; i < modeCount; i++)
            {
                var mode = modes[i];
                var adapter = mode.adapterId;
                _logger.LogInformation(
                    $"[Mode {i}] Type={mode.infoType}, Id={mode.id}, Adapter={adapter.LowPart}/{adapter.HighPart}");

                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.SOURCE)
                {
                    var src = mode.sourceMode;
                    _logger.LogInformation(
                        $"  SourceMode: {src.width}x{src.height}, Pos=({src.position.x},{src.position.y})");
                }
                else if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.TARGET)
                {
                    var tgt = mode.targetMode.targetVideoSignalInfo;
                    _logger.LogInformation(
                        $"  TargetMode: {tgt.activeSize.cx}x{tgt.activeSize.cy}, Refresh={tgt.vSyncFreq.Numerator}/{tgt.vSyncFreq.Denominator}");
                }
            }

            _logger.LogInformation("[Dump] Done.");
        }

        public void Test_TryApplyDisplaySettings()
        {
            var allTargets = GetAllDisplayTargets();
            if (allTargets.Count == 0)
            {
                _logger.LogWarning("[Test] No targets found.");
                return;
            }

            var (adapterId, sourceId, targetId, available) = allTargets[0];
            _logger.LogInformation(
                $"[Test] Trying to set display as primary: Adapter={adapterId.LowPart}/{adapterId.HighPart}, SourceId={sourceId}, TargetId={targetId}");

            bool result = TryApplyDisplaySettings(adapterId, sourceId, targetId, 0, 0);
            if (result)
                _logger.LogInformation("[Test] Set as primary successful.");
            else
                _logger.LogWarning("[Test] Set as primary failed.");
        }

        public void Test_TryApplyDisplaySettingsWithParams(
            uint width,
            uint height,
            uint refreshNumerator,
            uint refreshDenominator,
            int positionX,
            int positionY)
        {
            var targets = GetAllDisplayTargets();
            if (targets.Count == 0)
            {
                _logger.LogWarning("[Test] No display targets found.");
                return;
            }

            var (adapterId, sourceId, targetId, available) = targets[0];
            _logger.LogInformation(
                $"[Test] Applying display config to: Adapter={adapterId.LowPart}/{adapterId.HighPart}, SourceId={sourceId}, TargetId={targetId}");

            // 获取现有路径和模式
            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0) return;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount,
                paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0) return;

            var matchedPath = paths.FirstOrDefault(p =>
                p.sourceInfo.adapterId.Equals(adapterId) &&
                p.sourceInfo.id == sourceId &&
                p.targetInfo.id == targetId &&
                p.targetInfo.targetAvailable);

            if (matchedPath.sourceInfo.adapterId.LowPart == 0 && matchedPath.sourceInfo.adapterId.HighPart == 0)
            {
                _logger.LogWarning("[Test] No matching path.");
                return;
            }

            var sourceMode = modes[matchedPath.sourceInfo.modeInfoIdx];
            var targetMode = modes[matchedPath.targetInfo.modeInfoIdx];

            // 更新 source 位置和分辨率
            sourceMode.sourceMode.position = new POINTL { x = positionX, y = positionY };
            sourceMode.sourceMode.width = width;
            sourceMode.sourceMode.height = height;

            // 更新 target 模式分辨率 + 刷新率
            targetMode.targetMode.targetVideoSignalInfo.activeSize = new SIZE { cx = width, cy = height };
            targetMode.targetMode.targetVideoSignalInfo.vSyncFreq = new DISPLAYCONFIG_RATIONAL
            {
                Numerator = refreshNumerator,
                Denominator = refreshDenominator
            };

            // 构造路径
            var newPath = new DISPLAYCONFIG_PATH_INFO
            {
                sourceInfo = new DISPLAYCONFIG_PATH_SOURCE_INFO
                {
                    adapterId = adapterId,
                    id = sourceId,
                    modeInfoIdx = 0
                },
                targetInfo = new DISPLAYCONFIG_PATH_TARGET_INFO
                {
                    adapterId = adapterId,
                    id = targetId,
                    modeInfoIdx = 1,
                    outputTechnology = matchedPath.targetInfo.outputTechnology,
                    refreshRate = new DISPLAYCONFIG_RATIONAL
                    {
                        Numerator = refreshNumerator,
                        Denominator = refreshDenominator
                    },
                    scaling = matchedPath.targetInfo.scaling,
                    rotation = matchedPath.targetInfo.rotation,
                    scanLineOrdering = matchedPath.targetInfo.scanLineOrdering,
                    targetAvailable = true
                },
                flags = DISPLAYCONFIG_PATH_ACTIVE
            };

            var newModes = new DISPLAYCONFIG_MODE_INFO[] { sourceMode, targetMode };

            result = NativeDisplayApi.SetDisplayConfig(
                1, new[] { newPath },
                2, newModes,
                SetDisplayConfigFlags.SDC_APPLY | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG
            );

            if (result == 0)
            {
                _logger.LogInformation("[Test] SetDisplayConfig applied successfully.");
            }
            else
            {
                _logger.LogWarning($"[Test] SetDisplayConfig failed: {result}");
            }
        }


        public bool TryApplyDisplaySettings(
            LUID adapterId,
            uint sourceId,
            uint targetId,
            int positionX = 0,
            int positionY = 0)
        {
            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0)
            {
                _logger.LogWarning($"[TryApplyDisplaySettings] GetDisplayConfigBufferSizes failed: {result}");
                return false;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS, ref pathCount,
                paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0)
            {
                _logger.LogWarning($"[TryApplyDisplaySettings] QueryDisplayConfig failed: {result}");
                return false;
            }

            var matchedPath = paths.FirstOrDefault(p =>
                p.sourceInfo.adapterId.Equals(adapterId) &&
                p.sourceInfo.id == sourceId &&
                p.targetInfo.id == targetId &&
                p.targetInfo.targetAvailable);

            if (matchedPath.sourceInfo.adapterId.LowPart == 0 && matchedPath.sourceInfo.adapterId.HighPart == 0)
            {
                _logger.LogWarning("[TryApplyDisplaySettings] Matching path not found.");
                return false;
            }

            // 复制现有 path 并修改 position
            var sourceModeIdx = matchedPath.sourceInfo.modeInfoIdx;
            var targetModeIdx = matchedPath.targetInfo.modeInfoIdx;

            if (sourceModeIdx >= modeCount || targetModeIdx >= modeCount)
            {
                _logger.LogWarning("[TryApplyDisplaySettings] Mode info index out of range.");
                return false;
            }

            var sourceMode = modes[sourceModeIdx];
            var targetMode = modes[targetModeIdx];

            // 修改 source position
            sourceMode.sourceMode.position = new POINTL { x = positionX, y = positionY };

            var newPath = new DISPLAYCONFIG_PATH_INFO
            {
                sourceInfo = new DISPLAYCONFIG_PATH_SOURCE_INFO
                {
                    adapterId = adapterId,
                    id = sourceId,
                    modeInfoIdx = 0,
                    statusFlags = 0
                },
                targetInfo = new DISPLAYCONFIG_PATH_TARGET_INFO
                {
                    adapterId = adapterId,
                    id = targetId,
                    modeInfoIdx = 1,
                    outputTechnology = matchedPath.targetInfo.outputTechnology,
                    refreshRate = matchedPath.targetInfo.refreshRate,
                    scaling = matchedPath.targetInfo.scaling,
                    rotation = matchedPath.targetInfo.rotation,
                    scanLineOrdering = matchedPath.targetInfo.scanLineOrdering,
                    targetAvailable = true,
                    statusFlags = 0
                },
                flags = DISPLAYCONFIG_PATH_ACTIVE
            };

            var newModes = new DISPLAYCONFIG_MODE_INFO[2];
            newModes[0] = sourceMode;
            newModes[1] = targetMode;

            result = NativeDisplayApi.SetDisplayConfig(
                1, new[] { newPath },
                2, newModes,
                SetDisplayConfigFlags.SDC_APPLY | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG);

            if (result != 0)
            {
                _logger.LogWarning($"[TryApplyDisplaySettings] SetDisplayConfig failed: {result}");
                return false;
            }

            _logger.LogInformation("[TryApplyDisplaySettings] Successfully applied.");
            return true;
        }


        public bool TrySetResolutionAndRefreshRate(
            LUID adapterId,
            uint sourceId,
            uint targetId,
            uint width,
            uint height,
            uint refreshNumerator,
            uint refreshDenominator,
            SetDisplayConfigFlags flags =
                SetDisplayConfigFlags.SDC_APPLY | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG)
        {
            _logger.LogInformation(
                $"[SetResolution] Request: adapter={adapterId.LowPart}/{adapterId.HighPart}, sourceId={sourceId}, targetId={targetId}, {width}x{height} @ {refreshNumerator}/{refreshDenominator}");

            uint pathCount = 0, modeCount = 0;
            int result = NativeDisplayApi.GetDisplayConfigBufferSizes(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, ref modeCount);
            if (result != 0)
            {
                _logger.LogWarning($"[SetResolution] GetDisplayConfigBufferSizes failed: {result}");
                return false;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            result = NativeDisplayApi.QueryDisplayConfig(DisplayConfigConstants.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0)
            {
                _logger.LogWarning($"[SetResolution] QueryDisplayConfig failed: {result}");
                return false;
            }

            var path = paths.FirstOrDefault(p =>
                p.sourceInfo.adapterId.Equals(adapterId) &&
                p.sourceInfo.id == sourceId &&
                p.targetInfo.id == targetId &&
                p.targetInfo.targetAvailable);

            if (path.sourceInfo.adapterId.LowPart == 0 && path.sourceInfo.adapterId.HighPart == 0)
            {
                _logger.LogWarning("[SetResolution] Target path not found.");
                return false;
            }

            var sourceModeIdx = path.sourceInfo.modeInfoIdx;
            var targetModeIdx = path.targetInfo.modeInfoIdx;

            if (sourceModeIdx >= modes.Length || targetModeIdx >= modes.Length)
            {
                _logger.LogWarning("[SetResolution] Mode index out of range.");
                return false;
            }

            // 🧱 复制并准备新结构体
            var newPaths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            Array.Copy(paths, newPaths, pathCount);

            var newModes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            Array.Copy(modes, newModes, modeCount);

            var pathToUpdate = newPaths.FirstOrDefault(p =>
                p.sourceInfo.adapterId.Equals(adapterId) &&
                p.sourceInfo.id == sourceId &&
                p.targetInfo.id == targetId &&
                p.targetInfo.targetAvailable);

            if (pathToUpdate.sourceInfo.adapterId.LowPart == 0 && pathToUpdate.sourceInfo.adapterId.HighPart == 0)
            {
                _logger.LogWarning("[SetResolution] Failed to locate path to update.");
                return false;
            }

            var targetMode = newModes[pathToUpdate.targetInfo.modeInfoIdx];

            // ✅ 设置必要字段
            targetMode.infoType = DISPLAYCONFIG_MODE_INFO_TYPE.TARGET;
            targetMode.adapterId = adapterId;
            targetMode.id = targetId;

            // ⚙️ 修改 VideoSignalInfo
            var videoSignalInfo = targetMode.targetMode.targetVideoSignalInfo;
            videoSignalInfo.activeSize = new SIZE { cx = width, cy = height };
            videoSignalInfo.totalSize = videoSignalInfo.activeSize;
            videoSignalInfo.vSyncFreq = new DISPLAYCONFIG_RATIONAL
            {
                Numerator = refreshNumerator,
                Denominator = refreshDenominator
            };
            videoSignalInfo.hSyncFreq = new DISPLAYCONFIG_RATIONAL
            {
                Numerator = refreshNumerator * width, // 粗略估算
                Denominator = refreshDenominator
            };
            videoSignalInfo.pixelRate = (ulong)(width * height * refreshNumerator); // 粗估
            videoSignalInfo.scanLineOrdering = DISPLAYCONFIG_SCANLINE_ORDERING.UNSPECIFIED;
            videoSignalInfo.videoStandard = 255;

            // 🔁 回写
            targetMode.targetMode.targetVideoSignalInfo = videoSignalInfo;
            newModes[pathToUpdate.targetInfo.modeInfoIdx] = targetMode;

            // ✅ 应用完整结构
            result = NativeDisplayApi.SetDisplayConfig(
                newPaths.Length, newPaths,
                newModes.Length, newModes,
                flags
            );

            if (result != 0)
            {
                _logger.LogWarning($"[SetResolution] SetDisplayConfig failed: {result}");
                return false;
            }

            _logger.LogInformation("[SetResolution] Resolution/refresh applied successfully.");
            return true;
        }


        public void Test_TrySetResolutionAndRefreshRate()
        {
            var targets = GetAllDisplayTargets();
            if (targets.Count == 0)
            {
                _logger.LogWarning("[Test] No display targets found.");
                return;
            }

            var (adapterId, sourceId, targetId, available) = targets[0];
            _logger.LogInformation($"[Test] Setting resolution to 1920x1080 @60Hz on TargetId={targetId}");

            TrySetResolutionAndRefreshRate(
                adapterId,
                sourceId,
                targetId,
                width: 1920,
                height: 1080,
                refreshNumerator: 60,
                refreshDenominator: 1
            );
        }


        public void TestDisplayTargets()
        {
            var targets = GetAllDisplayTargets();
            _logger.LogInformation($"[Test] Total targets: {targets.Count}");

            int index = 0;
            foreach (var (adapterId, sourceId, targetId, available) in targets)
            {
                _logger.LogInformation(
                    $"[Target #{index}] AdapterId={adapterId.LowPart}/{adapterId.HighPart}, SourceId={sourceId}, TargetId={targetId}, Available={available}");

                var info = GetDisplayTargetInfo(adapterId, targetId);
                if (info.HasValue)
                {
                    var (devicePath, friendlyName) = info.Value;
                    _logger.LogInformation($"           DevicePath={devicePath}");
                    _logger.LogInformation($"           FriendlyName={friendlyName}");
                }
                else
                {
                    _logger.LogWarning($"           Failed to get target info for TargetId={targetId}");
                }

                index++;
            }

            DumpQueryDisplayConfigInfo();

            //Test_TryApplyDisplaySettings();

            // Test_TryApplyDisplaySettingsWithParams(
            //     width: 1920,
            //     height: 1080,
            //     refreshNumerator: 60,
            //     refreshDenominator: 1,
            //     positionX: 0,
            //     positionY: 0
            // );

            Test_TrySetResolutionAndRefreshRate();
        }

        #region api

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
            public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
        {
            DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
            DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
            DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
            DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
            DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
            DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6
        }

        public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
        {
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
            DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
        {
            public uint value;
        }

        [Flags]
        public enum SetDisplayConfigFlags : uint
        {
            SDC_TOPOLOGY_INTERNAL = 0x00000001,
            SDC_TOPOLOGY_CLONE = 0x00000002,
            SDC_TOPOLOGY_EXTEND = 0x00000004,
            SDC_TOPOLOGY_EXTERNAL = 0x00000008,
            SDC_TOPOLOGY_SUPPLIED = 0x00000010,
            SDC_USE_DATABASE_CURRENT = 0x00000200,
            SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020,
            SDC_VALIDATE = 0x00000040,
            SDC_APPLY = 0x00000080,
            SDC_NO_OPTIMIZATION = 0x00000100,
            SDC_ALLOW_CHANGES = 0x00000400,
            SDC_SAVE_TO_DATABASE = 0x00000800,
            SDC_ALLOW_PATH_ORDER_CHANGES = 0x00001000,
            SDC_VIRTUAL_MODE_AWARE = 0x00002000,
            SDC_USE_SUPPLIED_DEVICE_PATHS = 0x00010000
        }

        public const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;

        [Flags]
        public enum DisplayConfigPathFlags : uint
        {
            DISPLAYCONFIG_PATH_ACTIVE = 0x00000001
        }

        public enum DISPLAYCONFIG_ROTATION : uint
        {
            IDENTITY = 1, // No rotation
            ROTATE90 = 2,
            ROTATE180 = 3,
            ROTATE270 = 4
        }

        public enum DISPLAYCONFIG_SCALING : uint
        {
            IDENTITY = 1, // 1:1 无缩放
            CENTERED = 2,
            STRETCHED = 3,
            ASPECTRATIOCENTEREDMAX = 4,
            CUSTOM = 5,
            PREFERRED = 128
        }

        public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
        {
            UNSPECIFIED = 0,
            PROGRESSIVE = 1,
            INTERLACED = 2,
            INTERLACED_UPPERFIELDFIRST = 3,
            INTERLACED_LOWERFIELDFIRST = 4,
            DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public uint cx; // Width
            public uint cy; // Height
        }

        #endregion
    }
}