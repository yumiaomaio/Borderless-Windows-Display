using System.IO;
using System.Text.Json;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;
using BorderlessWindowApp.Models;

namespace BorderlessWindowApp.Services.WindowStyle
{
    /// <summary>
    /// 管理窗口样式预设（内置 + 可自定义）
    /// </summary>
    public class WindowStylePresetManager : IWindowStylePresetManager
    {
        private readonly Dictionary<string, WindowStylePresetConfig> _presets;

        public WindowStylePresetManager()
        {
            _presets = new Dictionary<string, WindowStylePresetConfig>(StringComparer.OrdinalIgnoreCase);
            RegisterBuiltInPresets();
        }

        /// <summary>
        /// 获取所有可用预设的键名
        /// </summary>
        public IEnumerable<string> GetAllPresetKeys() => _presets.Keys;

        /// <summary>
        /// 获取指定键的预设项（找不到时抛出异常）
        /// </summary>
        public WindowStylePresetConfig GetPreset(string key)
        {
            if (!_presets.TryGetValue(key, out var config))
                throw new ArgumentException($"找不到窗口预设：{key}");

            return config;
        }

        /// <summary>
        /// 尝试获取指定预设（推荐方式）
        /// </summary>
        public bool TryGetPreset(string key, out WindowStylePresetConfig config) =>
            _presets.TryGetValue(key, out config);

        /// <summary>
        /// 注册一个新预设，支持覆盖
        /// </summary>
        public void RegisterPreset(string key, WindowStylePresetConfig config, bool overwrite = false)
        {
            if (_presets.ContainsKey(key) && !overwrite)
                throw new InvalidOperationException($"预设已存在：{key}");

            _presets[key] = config;
        }

        /// <summary>
        /// 注册默认内置的所有窗口风格
        /// </summary>
        private void RegisterBuiltInPresets()
        {
            RegisterPreset("Standard", new WindowStylePresetConfig(
                style: WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_CLIENTEDGE | WindowExStyles.WS_EX_STATICEDGE,
                description: "标准窗口：有边框、标题栏、系统按钮"));

            RegisterPreset("Borderless", new WindowStylePresetConfig(
                style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.None,
                description: "无边框窗口：适合自绘界面"));

            RegisterPreset("ToolWindow", new WindowStylePresetConfig(
                style: WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_TOOLWINDOW | WindowExStyles.WS_EX_TOPMOST,
                description: "工具窗口：无任务栏图标，自动置顶",
                alwaysTopmost: true,
                allowResize: false));

            RegisterPreset("Overlay", new WindowStylePresetConfig(
                style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TRANSPARENT | WindowExStyles.WS_EX_TOPMOST,
                description: "透明叠加层：穿透、置顶",
                alwaysTopmost: true,
                allowResize: false,
                transparency: 0.6));

            RegisterPreset("Dialog", new WindowStylePresetConfig(
                style: WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_DLGMODALFRAME,
                description: "对话框风格：简洁标题栏",
                allowResize: false));

            RegisterPreset("FullScreen", new WindowStylePresetConfig(
                style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_TOPMOST,
                description: "全屏窗口：无边框、置顶",
                alwaysTopmost: true,
                allowResize: false));

            RegisterPreset("Popup", new WindowStylePresetConfig(
                style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_TOOLWINDOW,
                description: "弹出窗口：适合提示、菜单",
                allowResize: false));

            RegisterPreset("DebugOverlay", new WindowStylePresetConfig(
                style: WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                exStyle: WindowExStyles.WS_EX_LAYERED | WindowExStyles.WS_EX_TOPMOST,
                description: "调试浮层：半透明可点击",
                alwaysTopmost: true,
                transparency: 0.8));
        }
        
        private const string PresetConfigFile = "user-presets.json";
        
        public void LoadUserPresetsFromFile(string? path = null)
        {
            var filePath = path ?? PresetConfigFile;
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var userPresets = JsonSerializer.Deserialize<Dictionary<string, WindowStylePresetConfig>>(json);

            if (userPresets != null)
            {
                foreach (var (key, config) in userPresets)
                {
                    RegisterPreset(key, config, overwrite: true);
                }
            }
        }

        public void SaveUserPresetsToFile(string? path = null)
        {
            var filePath = path ?? PresetConfigFile;

            // 仅保存非内置的预设
            var userPresets = _presets
                .Where(kvp => !_builtInKeys.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var json = JsonSerializer.Serialize(userPresets, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
        
        private readonly HashSet<string> _builtInKeys = new(StringComparer.OrdinalIgnoreCase);

        private void RegisterPreset(string key, WindowStylePresetConfig config, bool overwrite, bool isBuiltIn)
        {
            if (_presets.ContainsKey(key) && !overwrite) return;

            _presets[key] = config;
            if (isBuiltIn)
                _builtInKeys.Add(key);
        }
        
    }
}
