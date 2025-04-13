using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Models
{
    public record WindowStylePresetConfig(
        WindowStyles Style,
        WindowExStyles ExStyle,
        string Description,
        bool AlwaysTopmost = false,
        bool AllowResize = true,
        double? Transparency = null
    );
}