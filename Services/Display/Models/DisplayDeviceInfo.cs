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
    public string ComboBoxDisplayText { get => FormatComboBoxItemText(this); }

    #region Tools
    private string FormatComboBoxItemText(DisplayDeviceInfo deviceInfo)
    {
        if (deviceInfo == null) return "Invalid Device";
        string displayNum = ExtractDisplayNumber(deviceInfo.DeviceName);
        string tech = FormatOutputTechnology(deviceInfo.OutputTechnology);
        // Format: [SourceId] [DisplayNum] FriendlyName (OutputTech)
        return $"[{deviceInfo.SourceId}] [{displayNum}] {deviceInfo.FriendlyName ?? "Unknown"} ({tech})";
    }
    private string FormatOutputTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY tech)
    {
        // Remove the prefix and handle specific cases
        string name = tech.ToString().Replace("DISPLAYCONFIG_OUTPUT_TECHNOLOGY_", "");
        switch (name)
        {
            case "OTHER": return "Other";
            case "HD15": return "VGA"; // More common name
            case "DVI": return "DVI";
            case "HDMI": return "HDMI";
            case "LVDS": return "LVDS";
            case "SDI": return "SDI";
            case "DISPLAYPORT_EXTERNAL": return "DP";
            case "DISPLAYPORT_EMBEDDED": return "eDP";
            case "UDI_EXTERNAL": return "UDI";
            case "UDI_EMBEDDED": return "eUDI";
            case "SDTV_DONGLE": return "SDTV";
            case "MIRACAST": return "Miracast";
            case "INDIRECT_WIRED": return "Wired"; // Simplified
            case "INDIRECT_VIRTUAL": return "Virtual"; // Simplified
            case "INTERNAL": return "Internal";
            // Add other cases as needed based on the full enum definition
            default:
                // Attempt to clean up unknown values (e.g., _INTERNAL)
                if (name.Contains("_"))
                    name = name.Substring(name.LastIndexOf('_') + 1);
                return name; // Return cleaned name or original if no underscore
        }
    }
    private string ExtractDisplayNumber(string? deviceName)
    {
        // Extracts the number from "\\.\DISPLAY1" -> "1"
        if (string.IsNullOrWhiteSpace(deviceName) || !deviceName.StartsWith(@"\\.\DISPLAY"))
        {
            return "?";
        }
        return deviceName.Substring(@"\\.\DISPLAY".Length);
    }

    #endregion
    
}
