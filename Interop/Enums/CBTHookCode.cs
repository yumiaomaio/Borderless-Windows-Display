using System;

namespace BorderlessWindowApp.Interop.Enums
{
    public enum CBTHookCode
    {
        /// <summary>被动挂钩：用于初始化。</summary>
        HCBT_MOVESIZE = 0,
        /// <summary>即将最小化、最大化、还原等操作。</summary>
        HCBT_MINMAX = 1,
        /// <summary>窗口将被创建。</summary>
        HCBT_CREATEWND = 3,
        /// <summary>窗口将被激活。</summary>
        HCBT_ACTIVATE = 5,
        /// <summary>窗口将被关闭（销毁）。</summary>
        HCBT_DESTROYWND = 4,
        /// <summary>用户点击了系统菜单中的命令。</summary>
        HCBT_SYSCOMMAND = 9,
        /// <summary>用户切换焦点窗口。</summary>
        HCBT_SETFOCUS = 10
    }
}