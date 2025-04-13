using BorderlessWindowApp.Models;

namespace BorderlessWindowApp.Services.WindowStyle
{
    public interface IWindowStylePresetManager
    {
        IEnumerable<string> GetAllPresetKeys();
        WindowStylePresetConfig GetPreset(string key);
        void RegisterPreset(string key, WindowStylePresetConfig config, bool overwrite = false);
        bool TryGetPreset(string key, out WindowStylePresetConfig config);
    }
}