namespace BorderlessWindowApp.Interop.Enums.Display
{
    public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : int
    {
        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
        DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
        DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
        GET_DPI_SCALE = -3,
        SET_DPI_SCALE = -4
    }

    public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
    {
        SOURCE = 1,
        TARGET = 2
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