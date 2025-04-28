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
    
    public const int CDS_UPDATEREGISTRY = 0x00000001;
    public const int CDS_GLOBAL = 0x00000008; // Apply settings to all users
    public const int CDS_SET_PRIMARY = 0x00000010;
    public const int CDS_NORESET = 0x10000000; // Don't restart (usually desired)
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