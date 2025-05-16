using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.ViewModels.Pages
{
    public partial class FavoritesPageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private FavoritesListViewModel _favoritesList;

        [ObservableProperty]
        private FavoriteDetailViewModel _favoriteDetail;

        public FavoritesPageViewModel()
        {
            // 构造函数仅创建必要对象
            _favoritesList = new FavoritesListViewModel();
            _favoriteDetail = new FavoriteDetailViewModel();
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            // 监听列表选择变化
            _favoritesList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FavoritesListViewModel.SelectedItem) && _favoritesList.SelectedItem != null)
                {
                    // 更新详情内容
                    _favoriteDetail.Title = _favoritesList.SelectedItem.Title;
                    _favoriteDetail.DateAdded = _favoritesList.SelectedItem.DateAdded;
                    _favoriteDetail.Content = _favoritesList.SelectedItem.Content;
                }
            };

            // 添加模拟数据
            AddMockData();
            
            _isInitialized = true;
        }

        private void AddMockData()
        {
            _favoritesList.Items.Add(new FavoriteItemViewModel 
            { 
                Title = "重要笔记", 
                Description = "会议记录和待办事项", 
                Icon = SymbolRegular.Notebook24,
                DateAdded = "2025-05-10",
                Content = "这是一个重要的会议笔记，包含了以下内容：\n\n1. 项目进度更新\n2. 下一步工作计划\n3. 资源分配情况\n\n请记得在下周一之前完成相关任务。"
            });
            
            _favoritesList.Items.Add(new FavoriteItemViewModel 
            { 
                Title = "技术文档", 
                Description = "API接口说明", 
                Icon = SymbolRegular.Document24,
                DateAdded = "2025-05-12",
                Content = "API接口文档\n\n请求地址：https://api.example.com/data\n请求方式：GET\n参数说明：\n- id: 资源ID\n- token: 验证令牌\n\n返回格式：JSON\n"
            });
            
            _favoritesList.Items.Add(new FavoriteItemViewModel 
            { 
                Title = "链接收藏", 
                Description = "常用网站链接", 
                Icon = SymbolRegular.Link24,
                DateAdded = "2025-05-15",
                Content = "常用链接：\n\n1. Github: https://github.com\n2. Stack Overflow: https://stackoverflow.com\n3. Microsoft Learn: https://learn.microsoft.com"
            });
            
            // 默认选中第一项
            if (_favoritesList.Items.Count > 0)
            {
                _favoritesList.SelectedItem = _favoritesList.Items[0];
            }
        }
    }

    public partial class FavoritesListViewModel : ObservableObject
    {
        public ObservableCollection<FavoriteItemViewModel> Items { get; } = new();

        [ObservableProperty]
        private FavoriteItemViewModel _selectedItem;
    }

    public partial class FavoriteItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private SymbolRegular _icon;

        [ObservableProperty]
        private string _dateAdded;

        [ObservableProperty]
        private string _content;
    }

    public partial class FavoriteDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _dateAdded;

        [ObservableProperty]
        private string _content;
    }
}