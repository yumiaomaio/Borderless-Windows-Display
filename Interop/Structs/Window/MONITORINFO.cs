using System.Runtime.InteropServices;

namespace BorderlessWindowApp.Interop.Structs.Window
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}