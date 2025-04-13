using System;
using System.Collections.Generic;

namespace BorderlessWindowApp.Services.Window
{
    public interface IWindowQueryService
    {
        /// <summary>
        /// 查找标题中包含指定字符串的窗口句柄（忽略大小写）
        /// </summary>
        IntPtr? FindByTitle(string title);

        /// <summary>
        /// 获取所有当前可见窗口的标题列表
        /// </summary>
        List<string> GetAllVisibleWindowTitles();
    }
}