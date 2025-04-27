namespace BorderlessWindowApp.Interop.Constants;

public static class ChangeDisplayConstants
{
    public const int DISP_CHANGE_SUCCESSFUL = 0;
    public const int DISP_CHANGE_RESTART = 1;
    public const int DISP_CHANGE_FAILED = -1;
    public const int DISP_CHANGE_BADMODE = -2;
    public const int DISP_CHANGE_NOTUPDATED = -3;
    public const int DISP_CHANGE_BADFLAGS = -4;
    public const int DISP_CHANGE_BADPARAM = -5;
    public const int DISP_CHANGE_BADDUALVIEW = -6;
}
public static class DisplayOrientationConstants
{
    public const uint DMDO_DEFAULT = 0;
    public const uint DMDO_90 = 1;
    public const uint DMDO_180 = 2;
    public const uint DMDO_270 = 3;
}
public static class DisplayConfigConstants
{
    public const int QDC_ALL_PATHS = 1;
    public const int QDC_ONLY_ACTIVE_PATHS = 2;
    public const int QDC_DATABASE_CURRENT = 4;
}
public static class IModeNum
{
    public const int ENUM_CURRENT_SETTINGS = -1;
    public const int ENUM_REGISTRY_SETTINGS = -2; 
}