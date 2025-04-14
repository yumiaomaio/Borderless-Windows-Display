using System.Text.Json.Serialization;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;

namespace BorderlessWindowApp.Services.WindowStyle
{
    public class WindowStylePresetConfig
    {
        public WindowStyles Style { get; set; }
        public WindowExStyles ExStyle { get; set; }
        public string Description { get; set; }
        public bool AlwaysTopmost { get; set; }
        public bool AllowResize { get; set; }
        public double? Transparency { get; set; }

        [JsonConstructor]
        public WindowStylePresetConfig() { }

        public WindowStylePresetConfig(
            WindowStyles style,
            WindowExStyles exStyle,
            string description,
            bool alwaysTopmost = false,
            bool allowResize = true,
            double? transparency = null)
        {
            Style = style;
            ExStyle = exStyle;
            Description = description;
            AlwaysTopmost = alwaysTopmost;
            AllowResize = allowResize;
            Transparency = transparency;
        }
    }
}