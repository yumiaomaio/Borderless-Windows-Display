using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace BorderlessWindowApp.Helpers
{
    public static class PrivilegeHelper
    {
        /// <summary>
        /// 判断当前进程是否已具有管理员权限
        /// </summary>
        public static bool IsRunAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 以管理员身份重新启动当前程序
        /// </summary>
        public static void RelaunchAsAdministratorAndExit()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;

            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas" // 触发 UAC 提权对话框
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show("权限被拒绝，应用无法继续运行。", "权限提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Environment.Exit(0); // 退出当前进程
        }

        /// <summary>
        /// 如果不是管理员，则自动提权重启并退出当前实例
        /// </summary>
        public static void EnsureRunAsAdministrator()
        {
            if (!IsRunAsAdministrator())
            {
                RelaunchAsAdministratorAndExit();
            }
        }
    }
}