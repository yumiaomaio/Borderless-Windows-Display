using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp.Services.WindowStyle
{
    public interface IWindowStyleManager
    {
        WindowStyles GetStyle(IntPtr hWnd);
        WindowExStyles GetExStyle(IntPtr hWnd);

        void SetStyle(IntPtr hWnd, WindowStyles style);
        void SetExStyle(IntPtr hWnd, WindowExStyles exStyle);

        void ApplyPreset(IntPtr hWnd, string presetKey);
        void ApplyStyleChanges(IntPtr hWnd);
    }
}