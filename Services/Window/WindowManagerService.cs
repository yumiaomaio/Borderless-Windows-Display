using System;
using System.Drawing;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Services.Window;
using BorderlessWindowApp.Services.WindowLayout;
using BorderlessWindowApp.Services.WindowStyle;

namespace BorderlessWindowApp.Services
{
    public class WindowManagerService : IWindowManagerService
    {
        private readonly IWindowQueryService _query;
        private readonly IWindowStyleManager _style;
        private readonly IWindowLayoutService _layout;
        private readonly IWindowHookService _hook;

        private IntPtr? _managedWindow;

        public WindowManagerService(
            IWindowQueryService query,
            IWindowStyleManager style,
            IWindowLayoutService layout,
            IWindowHookService hook)
        {
            _query = query;
            _style = style;
            _layout = layout;
            _hook = hook;
        }

        public void InitWindow(string titleKeyword, string stylePreset, int width, int height)
        {
            var hwnd = _query.FindByTitle(titleKeyword);
            if (hwnd is null) return;

            _managedWindow = hwnd;

            // 应用样式
            _style.ApplyPreset(hwnd.Value, stylePreset);

            // 设置大小和居中
            _layout.SetWindowLayout(hwnd.Value, new Size(width, height),
                new WindowLayoutOptions
                {
                    UseClientSize = true,
                    CenterToScreen = true
                });

            // 安装钩子
            _hook.AttachWinEventHook(hwnd.Value, msg => Console.WriteLine(msg));
        }

        public void FocusWindow(string titleKeyword)
        {
            var hwnd = _query.FindByTitle(titleKeyword);
            if (hwnd is null || !NativeWindowApi.IsWindow(hwnd.Value)) return;

            // 如果最小化则还原
            if (NativeWindowApi.IsIconic(hwnd.Value))
            {
                NativeWindowApi.ShowWindow(hwnd.Value, (int)ShowWindowCommand.Restore);
            }

            NativeWindowApi.SetForegroundWindow(hwnd.Value);
            NativeWindowApi.BringWindowToTop(hwnd.Value);
        }


        public void ApplyStyle(string titleKeyword, string presetKey)
        {
            var hwnd = _query.FindByTitle(titleKeyword);
            if (hwnd is not null)
            {
                _style.ApplyPreset(hwnd.Value, presetKey);
            }
        }

        public void Cleanup()
        {
            _hook.DetachWinEventHooks();
            _hook.DetachCbtHook();
            _managedWindow = null;
        }
    }
}
