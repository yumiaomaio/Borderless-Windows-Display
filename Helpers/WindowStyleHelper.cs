using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Helpers
{
    public static class WindowStyleHelper
    {
        public static WindowStyles GetStyle(IntPtr hWnd)
        {
            int style = NativeApi.GetWindowLong(hWnd, NativeApi.GWL_STYLE);
            return (WindowStyles)(uint)style;
        }

        public static WindowExStyles GetExStyle(IntPtr hWnd)
        {
            int exStyle = NativeApi.GetWindowLong(hWnd, NativeApi.GWL_EXSTYLE);
            return (WindowExStyles)(uint)exStyle;
        }

        public static void SetStyle(IntPtr hWnd, WindowStyles style)
        {
            NativeApi.SetWindowLong(hWnd, NativeApi.GWL_STYLE, (int)style);
        }

        public static void SetExStyle(IntPtr hWnd, WindowExStyles exStyle)
        {
            NativeApi.SetWindowLong(hWnd, NativeApi.GWL_EXSTYLE, (int)exStyle);
        }

        public static void AddStyle(IntPtr hWnd, WindowStyles styleToAdd)
        {
            var current = GetStyle(hWnd);
            SetStyle(hWnd, current | styleToAdd);
        }

        public static void RemoveStyle(IntPtr hWnd, WindowStyles styleToRemove)
        {
            var current = GetStyle(hWnd);
            SetStyle(hWnd, current & ~styleToRemove);
        }

        public static void AddExStyle(IntPtr hWnd, WindowExStyles exStyleToAdd)
        {
            var current = GetExStyle(hWnd);
            SetExStyle(hWnd, current | exStyleToAdd);
        }

        public static void RemoveExStyle(IntPtr hWnd, WindowExStyles exStyleToRemove)
        {
            var current = GetExStyle(hWnd);
            SetExStyle(hWnd, current & ~exStyleToRemove);
        }
    }
}
