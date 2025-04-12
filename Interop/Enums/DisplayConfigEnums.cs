using System;

namespace BorderlessWindowApp.Interop.Enums
{
    public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : int
    {
        GET_DPI_SCALE = -3,
        SET_DPI_SCALE = -4
    }

    public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
    {
        Source = 1,
        Target = 2
    }

    public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
    {
        Unspecified = 0,
        Progressive = 1,
        Interlaced = 2,
        InterlacedUpperFieldFirst = Interlaced,
        InterlacedLowerFieldFirst = 3,
    }

    public enum DISPLAYCONFIG_PIXELFORMAT : uint
    {
        PixelFormat8Bpp = 1,
        PixelFormat16Bpp = 2,
        PixelFormat24Bpp = 3,
        PixelFormat32Bpp = 4,
        PixelFormatNongdi = 5
    }
}