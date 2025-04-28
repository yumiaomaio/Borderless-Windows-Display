using System.Windows.Controls;
using BorderlessWindowApp.Services.Display.implement;
using BorderlessWindowApp.Services.Presets;
using BorderlessWindowApp.ViewModels.Display;
using Microsoft.Extensions.Logging.Abstractions;

namespace BorderlessWindowApp.Views
{
    public partial class DisplaySettingsView : UserControl
    {
        private readonly DisplaySettingsViewModel _viewModel;

        public DisplaySettingsView()
        {
            InitializeComponent();

            // 模拟依赖注入（真实项目应使用 DI 容器）

            // 1. 实例化基础服务 (假设构造函数如之前所示或无参数)
            //    (请确保 DisplayXxxService 的构造函数与您项目中的实际情况匹配)
            //    (如果服务实现需要 logger, 请传入 NullLogger 或真实 logger)
            var infoService = new DisplayInfoService(new NullLogger<DisplayInfoService>());
            var configService = new DisplayConfigService(new NullLogger<DisplayConfigService>());
            var scaleService = new DisplayScaleService(new NullLogger<DisplayScaleService>());
            var presetService = new DisplayPresetService(); // 假设构造函数不需要参数

            // 2. 实例化新的 Applicator 服务
            var applicator = new DisplaySettingsApplicator(configService, scaleService, infoService);

            // 3. 实例化新的子 ViewModel / Manager
            var deviceSelectorViewModel = new DeviceSelectorViewModel(infoService);
            var presetManagerViewModel = new PresetManagerViewModel(presetService);

            // 4. 实例化主 ViewModel, 传入所有依赖项
            //    (注意: DisplaySettingsViewModel 现在需要 Applicator, InfoService, ScaleService 以及两个子ViewModel)
            _viewModel = new DisplaySettingsViewModel(
                deviceSelectorViewModel,
                presetManagerViewModel,
                infoService, // 主ViewModel仍可能需要InfoService来加载详细信息
                scaleService, // 主ViewModel仍可能需要ScaleService来加载详细信息
                applicator
            );

            // 5. 将主 ViewModel 设置为 DataContext (通常在 View 的构造函数或加载事件中完成)
            this.DataContext = _viewModel;
            this.Loaded += DisplaySettingsView_Loaded;
        }

        private void DisplaySettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 页面加载完成时的额外操作可在这里进行
        }
    }
}