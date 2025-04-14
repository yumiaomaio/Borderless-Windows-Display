namespace BorderlessWindowApp.Services.Display.Models
{
    public class DpiScalingInfo
    {
        public uint Minimum { get; set; } = 100;
        public uint Maximum { get; set; } = 100;
        public uint Current { get; set; } = 100;
        public uint Recommended { get; set; } = 100;
        public bool IsInitialized { get; set; } = false;
    }
}