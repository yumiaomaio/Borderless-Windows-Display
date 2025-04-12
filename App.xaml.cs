using System.Windows;
using BorderlessWindowApp.Helpers;
using Application = System.Windows.Application;

namespace BorderlessWindowApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        PrivilegeHelper.EnsureRunAsAdministrator(); // 自动提权

        base.OnStartup(e);
    }
}