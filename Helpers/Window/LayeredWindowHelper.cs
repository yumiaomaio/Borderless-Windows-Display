using BorderlessWindowApp.Interop;

namespace BorderlessWindowApp.Helpers.Window
{
    public static class LayeredWindowHelper
    {
        private const uint LWA_COLORKEY = 0x00000001;
        private const uint LWA_ALPHA = 0x00000002;

        /// <summary>
        /// 设置窗口透明度（0.0 ~ 1.0）
        /// </summary>
        public static void SetTransparency(IntPtr hWnd, double alpha)
        {
            if (alpha < 0 || alpha > 1) return;

            byte level = (byte)(alpha * 255);
            Win32WindowApi.SetLayeredWindowAttributes(hWnd, 0, level, LWA_ALPHA);
        }
    }
}