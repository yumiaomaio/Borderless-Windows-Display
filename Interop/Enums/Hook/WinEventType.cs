
namespace BorderlessWindowApp.Interop.Enums.Hook
{
    [Flags]
    public enum WinEventType : uint
    {
        None = 0,

        // System-level
        Foreground = 0x0003,
        MinimizeStart = 0x0016,
        MinimizeEnd = 0x0017,

        // Object-level
        Destroy = 0x8001,
        Show = 0x8002,
        Hide = 0x8003,
        LocationChange = 0x800B,
        NameChange = 0x800C,

        // Movement / input
        MoveSizeStart = 0x000A,
        MoveSizeEnd = 0x000B,
        CaptureStart = 0x0008,
        CaptureEnd = 0x0009
    }
}
