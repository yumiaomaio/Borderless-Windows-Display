namespace BorderlessWindowApp.Interop.Enums.Window
{
    [Flags]
    public enum WindowExStyles : uint
    {
        None = 0x00000000,

        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_WINDOWEDGE = 0x00000100,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_LAYERED = 0x00080000,       // ✅ 你缺失的这个
        WS_EX_NOACTIVATE = 0x08000000
    }
}