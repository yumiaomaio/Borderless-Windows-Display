using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Helpers.Display;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;

namespace BorderlessWindowApp.Services
{
    public class DisplayManagerService
    {
        public record MonitorDpiInfo(
            string AdapterId,
            uint SourceId,
            uint Current,
            uint Recommended,
            uint Minimum,
            uint Maximum,
            bool IsInitialized
        );

        public record MonitorModeInfo(
            string AdapterId,
            uint SourceId,
            uint Width,
            uint Height,
            double RefreshRate
        );

        // 🔍 DisplayConfig 路径相关
        public List<DISPLAYCONFIG_PATH_INFO> GetAllDisplayPaths() =>
            DisplayHelper.QueryDisplayPathsAndModes(out var paths, out _, 0x00000001) ? paths : new();

        public List<DISPLAYCONFIG_MODE_INFO> GetAllModeInfos() =>
            DisplayHelper.QueryDisplayPathsAndModes(out _, out var modes, 0x00000001) ? modes : new();

        public List<(LUID adapterId, uint sourceId, uint targetId)> GetActiveDisplaySources()
        {
            var result = new List<(LUID, uint, uint)>();
            var seen = new HashSet<string>();

            foreach (var path in GetAllDisplayPaths())
            {
                if (!path.targetInfo.targetAvailable || path.flags == 0)
                    continue;

                var key = $"{path.sourceInfo.adapterId.HighPart:X8}-{path.sourceInfo.adapterId.LowPart:X8}-{path.sourceInfo.id}";
                if (seen.Contains(key)) continue;
                seen.Add(key);

                result.Add((path.sourceInfo.adapterId, path.sourceInfo.id, path.targetInfo.id));
            }

            return result;
        }

        // 🧠 DPI 相关
        public MonitorDpiInfo GetDpiInfo(LUID adapterId, uint sourceId)
        {
            var dpi = DpiHelper.GetDpiScalingInfo(adapterId, sourceId);
            return new MonitorDpiInfo(
                AdapterId: $"{adapterId.HighPart:X8}-{adapterId.LowPart:X8}",
                SourceId: sourceId,
                Current: dpi.Current,
                Recommended: dpi.Recommended,
                Minimum: dpi.Minimum,
                Maximum: dpi.Maximum,
                IsInitialized: dpi.IsInitialized
            );
        }

        public bool SetDpiScaling(LUID adapterId, uint sourceId, uint dpiPercent) =>
            DpiHelper.SetDpiScaling(adapterId, sourceId, dpiPercent);

        // 🖥 分辨率/刷新率信息
        public List<MonitorModeInfo> GetMonitorModes()
        {
            var modes = GetAllModeInfos();
            var result = new List<MonitorModeInfo>();

            foreach (var mode in modes)
            {
                string adapterKey = $"{mode.adapterId.HighPart:X8}-{mode.adapterId.LowPart:X8}";

                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Source)
                {
                    var src = mode.modeInfo.sourceMode;
                    result.Add(new MonitorModeInfo(adapterKey, mode.id, src.width, src.height, 0));
                }
                else if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target)
                {
                    var tgt = mode.modeInfo.targetMode.targetVideoSignalInfo;
                    double hz = tgt.vSyncFreq.Denominator > 0
                        ? (double)tgt.vSyncFreq.Numerator / tgt.vSyncFreq.Denominator
                        : 0;

                    result.Add(new MonitorModeInfo(adapterKey, mode.id, tgt.activeSize.cx, tgt.activeSize.cy, hz));
                }
            }

            return result;
        }
        
        public (int width, int height, int frequency)? GetCurrentResolution(string displayName)
        {
            return ResolutionHelper.GetCurrentDisplayMode(displayName);
        }

        // 🎯 使用 DisplayConfig 设置分辨率（LUID + TargetID）
        public bool TrySetResolutionAndRefreshRate(
            LUID adapterId,
            uint targetId,
            uint width,
            uint height,
            uint refreshNumerator,
            uint refreshDenominator,
            SetDisplayConfigFlags flags = SetDisplayConfigFlags.SDC_APPLY | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG)
        {
            var paths = GetAllDisplayPaths();
            var modes = GetAllModeInfos();

            for (int i = 0; i < modes.Count; i++)
            {
                var mode = modes[i];
                if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target &&
                    mode.adapterId.LowPart == adapterId.LowPart &&
                    mode.adapterId.HighPart == adapterId.HighPart &&
                    mode.id == targetId)
                {
                    var signal = mode.modeInfo.targetMode.targetVideoSignalInfo;
                    signal.activeSize.cx = width;
                    signal.activeSize.cy = height;
                    signal.vSyncFreq.Numerator = refreshNumerator;
                    signal.vSyncFreq.Denominator = refreshDenominator;
                    mode.modeInfo.targetMode.targetVideoSignalInfo = signal;
                    modes[i] = mode;

                    return Interop.DisplayConfigApi.SetDisplayConfig(
                        (uint)paths.Count,
                        paths.ToArray(),
                        (uint)modes.Count,
                        modes.ToArray(),
                        (uint)flags) == 0;
                }
            }

            return false;
        }

        public async Task<bool> TestResolutionChangeWithRevert(
            LUID adapterId,
            uint targetId,
            uint testWidth, uint testHeight, uint testHz,
            uint restoreWidth, uint restoreHeight, uint restoreHz,
            int timeoutSeconds = 10)
        {
            bool success = TrySetResolutionAndRefreshRate(adapterId, targetId, testWidth, testHeight, testHz * 1000, 1000);
            if (!success) return false;

            await Task.Delay(timeoutSeconds * 1000);
            return TrySetResolutionAndRefreshRate(adapterId, targetId, restoreWidth, restoreHeight, restoreHz * 1000, 1000);
        }

        public List<(uint Width, uint Height, double Hz)> GetResolutionOptions(LUID adapterId, uint targetId)
        {
            var modes = GetAllModeInfos();
            return modes
                .Where(m =>
                    m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target &&
                    m.adapterId.HighPart == adapterId.HighPart &&
                    m.adapterId.LowPart == adapterId.LowPart &&
                    m.id == targetId)
                .Select(m =>
                {
                    var sig = m.modeInfo.targetMode.targetVideoSignalInfo;
                    double hz = sig.vSyncFreq.Denominator > 0
                        ? (double)sig.vSyncFreq.Numerator / sig.vSyncFreq.Denominator
                        : 0;
                    return (sig.activeSize.cx, sig.activeSize.cy, hz);
                })
                .Distinct()
                .ToList();
        }

        // 🎯 分辨率设置（基于显示器名称，如 \\.\DISPLAY1）
        public List<string> GetDisplayDeviceNames() =>
            ResolutionHelper.GetAllDisplayDeviceNames();

        public List<(int width, int height, int frequency)> GetSupportedModes(string deviceName) =>
            ResolutionHelper.GetAllSupportedModes(deviceName);

        public bool TryChangeResolution(string deviceName, int width, int height, int hz) =>
            ResolutionHelper.TryChangeResolution(deviceName, width, height, hz);
    }
}
