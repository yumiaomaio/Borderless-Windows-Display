namespace BorderlessWindowApp.Services.Display.Models
{
    public class DisplayConfigRequest
    {
        public string DeviceName { get; set; } = string.Empty;

        public int Width { get; set; }
        public int Height { get; set; }

        public int RefreshRate { get; set; } = 60;
        public int BitDepth { get; set; } = 32;

        public int? PositionX { get; set; } = null;
        public int? PositionY { get; set; } = null;
        
        public DisplayOrientation? Orientation { get; set; } = null;
        public bool SetAsPrimary { get; set; } = false;
    }
    
    public enum DisplayOrientation // 定义方向枚举 (可以映射到 Windows 的 DMDO_xxx 值)
    {
        Landscape = 0,        // DMDO_DEFAULT
        Portrait = 1,         // DMDO_90 (顺时针旋转90度)
        LandscapeFlipped = 2, // DMDO_180
        PortraitFlipped = 3   // DMDO_270
    }
}