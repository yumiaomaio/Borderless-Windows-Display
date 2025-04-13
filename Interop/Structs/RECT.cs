using System.Runtime.InteropServices;

namespace BorderlessWindowApp.Interop.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}