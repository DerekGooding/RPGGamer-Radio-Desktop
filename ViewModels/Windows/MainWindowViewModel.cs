using System.Collections.ObjectModel;
using RPGGamer_Radio_Desktop.Views.Pages;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "WPF UI - RPGGamer_Radio_Desktop";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems =
        [
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Songs",
                Icon = new SymbolIcon { Symbol = SymbolRegular.MusicNote224 },
                TargetPageType = typeof(SongsPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems =
        [
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(SettingsPage)
            }
        ];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems =
        [
            new MenuItem { Header = "Home", Tag = "tray_home" }
        ];
    }
}
