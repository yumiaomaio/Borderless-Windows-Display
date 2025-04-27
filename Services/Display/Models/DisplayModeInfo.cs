namespace BorderlessWindowApp.Services.Display.Models
{
    public class DisplayModeInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public DisplayOrientation Orientation { get; set; } = DisplayOrientation.Landscape;
        public override string ToString() => $"{Width}x{Height}";
    }
}