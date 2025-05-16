using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "IMSystem";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "消息",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Chat24 },
                TargetPageType = typeof(Views.Pages.ChatPage)
            },
            new NavigationViewItem()
            {
                Content = "联系人",
                Icon = new SymbolIcon { Symbol = SymbolRegular.People24 },
                TargetPageType = typeof(Views.Pages.ContactsPage)
            },
            new NavigationViewItem()
            {
                Content = "收藏",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Star24 },
                TargetPageType = typeof(Views.Pages.FavoritesPage)
            },
            new NavigationViewItem()
            {
                Content = "文件",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Folder24 },
                TargetPageType = typeof(Views.Pages.FilesPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "显示主窗口", Tag = "tray_show" },
            new MenuItem { Header = "退出", Tag = "tray_exit" }
        };
    }
}
