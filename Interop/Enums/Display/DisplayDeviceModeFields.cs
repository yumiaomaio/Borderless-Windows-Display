namespace BorderlessWindowApp.Interop.Enums.Display;

[Flags]
public enum DisplayDeviceModeFields : uint
{
    Position = 0x00000020,
    BitsPerPel = 0x00040000,
    PelsWidth = 0x00080000,
    PelsHeight = 0x00100000,
    DisplayFrequency = 0x00400000
}
