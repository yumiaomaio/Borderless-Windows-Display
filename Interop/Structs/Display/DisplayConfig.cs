using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Services.Display;

namespace BorderlessWindowApp.Interop.Structs.Display
{
    #region LUID Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }
    #endregion

    #region DISPLAYCONFIG_DEVICE_INFO_HEADER Struct
    [StructLayout(LayoutKind.Sequential)]
    public partial struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }
    #endregion

    #region DISPLAYCONFIG_GET_DPI Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_GET_DPI
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int minScaleRel;
        public int curScaleRel;
        public int maxScaleRel;
    }
    #endregion

    #region DISPLAYCONFIG_SET_DPI Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SET_DPI
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int scaleRel;
    }
    #endregion

    #region DISPLAYCONFIG_PATH_SOURCE_INFO Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }
    #endregion

    #region DISPLAYCONFIG_PATH_TARGET_INFO Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public DisplayInfoService.DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
        public DisplayInfoService.DISPLAYCONFIG_ROTATION rotation;
        public DisplayInfoService.DISPLAYCONFIG_SCALING scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public DisplayInfoService.DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
        public bool targetAvailable;
        public uint statusFlags;
    }
    #endregion

    #region DISPLAYCONFIG_PATH_INFO Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }
    #endregion

    #region DISPLAYCONFIG_VIDEO_SIGNAL_INFO Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public DISPLAYCONFIG_RATIONAL hSyncFreq;
        public DISPLAYCONFIG_RATIONAL vSyncFreq;
        public DisplayInfoService.SIZE activeSize;
        public DisplayInfoService.SIZE totalSize;
        public uint videoStandard;
        public DisplayInfoService.DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
    }
    #endregion

    #region DISPLAYCONFIG_SOURCE_MODE Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint width;
        public uint height;
        public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
        public POINTL position;
    }
    #endregion

    #region DISPLAYCONFIG_TARGET_MODE Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }
    #endregion

    #region DISPLAYCONFIG_MODE_INFO_UNION Struct
    [StructLayout(LayoutKind.Explicit)]
    public struct DISPLAYCONFIG_MODE_INFO_UNION
    {
        [FieldOffset(0)]
        public DISPLAYCONFIG_TARGET_MODE targetMode;

        [FieldOffset(0)]
        public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }
    #endregion

    #region DISPLAYCONFIG_MODE_INFO Struct
    [StructLayout(LayoutKind.Explicit)]
    public struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)]
        public DISPLAYCONFIG_MODE_INFO_TYPE infoType;

        [FieldOffset(4)]
        public uint id;

        [FieldOffset(8)]
        public LUID adapterId;

        [FieldOffset(16)]
        public DISPLAYCONFIG_TARGET_MODE targetMode;

        [FieldOffset(16)]
        public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    #endregion

    #region DISPLAYCONFIG_RATIONAL Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }
    #endregion

    #region DISPLAYCONFIG_2DREGION Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }
    #endregion

    #region POINTL Struct
    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        public int x;
        public int y;
    }
    #endregion

    #region DEVMODE Struct
    [StructLayout(LayoutKind.Sequential, CharSet =CharSet.Unicode)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;

        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;

        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;

        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;

        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;

        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }
    #endregion

    #region DISPLAY_DEVICE Struct
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
    #endregion
    
}