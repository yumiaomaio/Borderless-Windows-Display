using System.Windows.Controls;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace BorderlessWindowApp
{
    public partial class DisplaySettingsView : UserControl
    {
        private readonly DisplaySettingsViewModel _viewModel;

        public DisplaySettingsView()
        {
            InitializeComponent();

            // 模拟依赖注入（真实项目应使用 DI 容器）
            var infoService = new DisplayInfoService(new NullLogger<DisplayInfoService>());
            var configService = new DisplayConfigService(new NullLogger<DisplayConfigService>());
            var scaleService = new DisplayScaleService(new NullLogger<DisplayScaleService>());

            _viewModel = new DisplaySettingsViewModel(infoService, configService, scaleService);
            this.DataContext = _viewModel;

            this.Loaded += DisplaySettingsView_Loaded;
        }

        private void DisplaySettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 页面加载完成时的额外操作可在这里进行
        }
    }
}