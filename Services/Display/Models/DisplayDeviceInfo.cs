using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs.Display;

namespace BorderlessWindowApp.Services.Display.Models;

public class DisplayDeviceInfo
{
    public LUID AdapterId { get; set; }
    public uint SourceId { get; set; }
    public uint TargetId { get; set; }
    public string? DeviceName { get; set; }           // \\.\DISPLAY1
    public string? DeviceString { get; set; }         // NVIDIA GeForce...
    public string? FriendlyName { get; set; }         // Dell U2723QE
    public string? DevicePath { get; set; }           // \\?\DISPLAY#...
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY OutputTechnology { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public bool IsAvailable { get; set; }
}
