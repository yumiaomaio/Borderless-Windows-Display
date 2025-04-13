using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Services;
using BorderlessWindowApp.Interop.Enums;

namespace BorderlessWindowApp
{
    public partial class MainWindow
    {
        private readonly WindowManagerService _manager = new();
        private IntPtr _targetHwnd = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowList();
            LoadStyleList();
        }

        private void LoadWindowList()
        {
            WindowComboBox.ItemsSource = _manager.GetAllVisibleWindowTitles();
        }

        private void LoadStyleList()
        {
            StyleComboBox.ItemsSource = Enum.GetValues(typeof(WindowStyleHelper.WindowStylePreset));
            StyleComboBox.SelectedIndex = 0;
        }

        private void ApplyStyle_Click(object sender, RoutedEventArgs e)
        {
            if (WindowComboBox.SelectedItem is not string title) return;
            if (StyleComboBox.SelectedItem is not WindowStyleHelper.WindowStylePreset preset) return;

            _targetHwnd = _manager.FindWindow(title);
            if (_targetHwnd == IntPtr.Zero)
            {
                Log("❌ 找不到窗口");
                return;
            }

            _manager.Snapshot(_targetHwnd);
            _manager.ApplyStyle(_targetHwnd, preset);
            _manager.CenterWindow(_targetHwnd);
            Log($"✅ 应用了样式：{preset} 到窗口：{title}");
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (_targetHwnd == IntPtr.Zero)
            {
                Log("⚠️ 没有缓存窗口快照");
                return;
            }

            _manager.RestoreSnapshot(_targetHwnd);
            Log("✅ 恢复了窗口状态");
        }

        private void Log(string msg)
        {
            LogBox.AppendText($"{DateTime.Now:HH:mm:ss} | {msg}\n");
            LogBox.ScrollToEnd();
        }
    }
}