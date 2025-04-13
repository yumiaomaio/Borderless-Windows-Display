namespace BorderlessWindowApp.Services.WindowLayout
{
    public enum RelativeMonitor
    {
        /// <summary>使用默认工作区（SystemParameters.WorkArea）</summary>
        Primary,

        /// <summary>根据窗口当前所在显示器计算</summary>
        Current,

        /// <summary>根据用户指定的显示器编号</summary>
        Specific
    }
}