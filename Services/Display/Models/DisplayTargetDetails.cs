using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs.Display;

namespace BorderlessWindowApp.Services.Display.Models;

public class DisplayTargetDetails
{
    public string? FriendlyName { get; set; }
    public string? DevicePath { get; set; }
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY OutputTechnology { get; set; }
    public ushort EdidManufactureId { get; set; }
    public ushort EdidProductCodeId { get; set; }
    public uint ConnectorInstance { get; set; }
}
