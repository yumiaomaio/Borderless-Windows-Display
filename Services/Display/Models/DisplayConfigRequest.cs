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

        public bool SetAsPrimary { get; set; } = false;
    }
}