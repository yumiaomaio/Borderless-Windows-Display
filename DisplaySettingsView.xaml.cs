using System.Windows;
using BorderlessWindowApp.Services.Display;
using BorderlessWindowApp.ViewModels;

namespace BorderlessWindowApp.Views
{
    public partial class DisplaySettingsView : Window
    {
        public DisplaySettingsView(
            IDisplayInfoService infoService,
            IDisplayConfigService configService,
            IDisplayScaleService scaleService)
        {
            InitializeComponent();

            // 创建 ViewModel，并绑定到 DataContext
            DataContext = new DisplaySettingsViewModel(infoService, configService, scaleService);
        }
    }
}