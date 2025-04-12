using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BorderlessWindowApp.Helpers;
using BorderlessWindowApp.Services;

namespace BorderlessWindowApp
{
    public partial class MainWindow : Window
    {
        private readonly DisplayManagerService service = new();
        private string currentDisplayName = string.Empty;
        private (int width, int height, int hz)? originalResolution;
        private uint? originalDpi;

        public MainWindow()
        {
            InitializeComponent();
            LoadDisplays();
        }

        private void LoadDisplays()
        {
            var names = service.GetDisplayDeviceNames();
            displayCombo.ItemsSource = names;
            if (names.Count > 0)
            {
                displayCombo.SelectedIndex = 0;
            }
        }

        private void displayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (displayCombo.SelectedItem is not string displayName) return;
            currentDisplayName = displayName;

            // 当前分辨率
            originalResolution = ResolutionHelper.GetCurrentDisplayMode(displayName);
            
            var allModes = service.GetSupportedModes(displayName)
                .OrderByDescending(m => m.width)
                .ThenByDescending(m => m.frequency)
                .Select(m => new ResolutionOption
                {
                    Width = m.width,
                    Height = m.height,
                    Hz = m.frequency
                }).ToList();

            resolutionCombo.ItemsSource = allModes;

            if (originalResolution is { } res)
            {
                var selected = allModes.FirstOrDefault(m => m.Width == res.width && m.Height == res.height && m.Hz == res.hz);
                resolutionCombo.SelectedItem = selected;
            }
            
            // 当前 DPI
            var sources = service.GetActiveDisplaySources();
            if (sources.Count > 0)
            {
                var (adapterId, sourceId, _) = sources[0];
                var dpiInfo = service.GetDpiInfo(adapterId, sourceId);
                originalDpi = dpiInfo.Current;

                var options = new List<uint> { 100, 125, 150, 175, 200 };
                dpiCombo.ItemsSource = options;
                dpiCombo.SelectedItem = dpiInfo.Current;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentDisplayName) || resolutionCombo.SelectedItem is not string selected) return;

            var cleaned = selected.Replace("Hz", "").Replace(" ", ""); // "1920x1080@60"
            var resParts = cleaned.Split(new[] { 'x', '@' });

            if (resParts.Length != 3 ||
                !int.TryParse(resParts[0], out int width) ||
                !int.TryParse(resParts[1], out int height) ||
                !int.TryParse(resParts[2], out int hz))
            {
                MessageBox.Show("❌ 无法解析所选分辨率格式");
                return;
            }

            bool ok = service.TryChangeResolution(currentDisplayName, width, height, hz);
            MessageBox.Show(ok ? "✅ 分辨率设置成功" : "❌ 设置失败");

            // 设置 DPI（可选）
            if (dpiCombo.SelectedItem is uint dpi)
            {
                var sources = service.GetActiveDisplaySources();
                if (sources.Count > 0)
                {
                    var (adapterId, sourceId, _) = sources[0];
                    bool dpiOk = service.SetDpiScaling(adapterId, sourceId, dpi);
                    if (!dpiOk) MessageBox.Show("❌ DPI 设置失败");
                }
            }
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentDisplayName) || originalResolution is not { } orig) return;

            service.TryChangeResolution(currentDisplayName, orig.width, orig.height, orig.hz);

            if (originalDpi.HasValue)
            {
                var sources = service.GetActiveDisplaySources();
                if (sources.Count > 0)
                {
                    var (adapterId, sourceId, _) = sources[0];
                    service.SetDpiScaling(adapterId, sourceId, originalDpi.Value);
                }
            }

            MessageBox.Show("↩ 设置已回退");
        }
    }
    
    public class ResolutionOption
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Hz { get; set; }

        public override string ToString() => $"{Width}x{Height} @ {Hz}Hz";
    }

}
