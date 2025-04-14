using System;

namespace BorderlessWindowApp.Services.Window
{
    public interface IWindowHookService
    {
        void AttachWinEventHook(IntPtr target, Action<string> onEvent);
        void DetachWinEventHooks();

        void AttachCbtHook(IntPtr target, Action<string>? onIntercept = null);
        void DetachCbtHook();
    }
}