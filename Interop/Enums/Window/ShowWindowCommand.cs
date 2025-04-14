namespace BorderlessWindowApp.Interop.Enums.Window;

public enum ShowWindowCommand
{
    /// <summary>
    /// 隐藏窗口并激活其他窗口（如果有）
    /// </summary>
    Hide = 0,

    /// <summary>
    /// 激活并显示窗口。如果窗口最小化或最大化，则恢复到原始大小和位置（常用于首次显示）
    /// </summary>
    ShowNormal = 1,

    /// <summary>
    /// 激活窗口并将其最小化
    /// </summary>
    ShowMinimized = 2,

    /// <summary>
    /// 激活窗口并将其最大化
    /// </summary>
    ShowMaximized = 3,

    /// <summary>
    /// 以窗口上次的大小和位置显示窗口，但不激活它
    /// </summary>
    ShowNoActivate = 4,

    /// <summary>
    /// 激活并显示窗口（与 ShowNormal 行为基本一致）
    /// </summary>
    Show = 5,

    /// <summary>
    /// 最小化窗口并激活下一个顶层窗口
    /// </summary>
    Minimize = 6,

    /// <summary>
    /// 最小化窗口但不激活
    /// </summary>
    ShowMinNoActive = 7,

    /// <summary>
    /// 以上次大小和位置显示窗口，不激活（NA = No Activate）
    /// </summary>
    ShowNA = 8,

    /// <summary>
    /// 恢复窗口（如果最小化或最大化）到正常显示状态，并激活它
    /// </summary>
    Restore = 9
}