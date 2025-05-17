using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Abstractions.Controls;

namespace IMSystem.Client.Ui.ViewModels.Pages
{
    public partial class ChatPageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ChatListViewModel _chatList;

        [ObservableProperty]
        private ChatDetailViewModel _chatDetail;

        [ObservableProperty]
        private bool _hasSelectedChat = false;

        public ChatPageViewModel()
        {
            // 空构造函数，初始化移至InitializeViewModel中
            _chatList = new ChatListViewModel();
            _chatDetail = new ChatDetailViewModel();
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
            // 监听聊天列表选择变化
            _chatList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChatListViewModel.SelectedItem))
                {
                    if (_chatList.SelectedItem != null)
                    {
                        // 更新聊天详情
                        _chatDetail.Title = _chatList.SelectedItem.Title;
                        _chatDetail.Messages.Clear();
                        
                        // 模拟消息数据
                        _chatDetail.Messages.Add(new MessageViewModel { Content = "你好，这是一条测试消息", IsFromMe = false });
                        _chatDetail.Messages.Add(new MessageViewModel { Content = "这是我的回复", IsFromMe = true });
                        _chatDetail.Messages.Add(new MessageViewModel { Content = "这是一个模拟的聊天界面，用于展示布局效果", IsFromMe = false });
                        
                        // 更新选中状态
                        HasSelectedChat = true;
                    }
                    else
                    {
                        // 没有选中项，显示欢迎界面
                        HasSelectedChat = false;
                    }
                }
            };

            // 添加模拟数据
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "张三", 
                LastMessage = "你好，在吗？", 
                AvatarText = "张",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy1.png",
                Time = "刚刚"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "李四", 
                LastMessage = "下午开会，别忘了", 
                AvatarText = "李",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy2.png",
                Time = "10:30"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "王五", 
                LastMessage = "我已经提交了代码", 
                AvatarText = "王",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy3.png",
                Time = "昨天"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "赵六", 
                LastMessage = "下班一起吃饭吧", 
                AvatarText = "赵",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy4.png",
                Time = "星期四"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "小红", 
                LastMessage = "项目进度如何了？", 
                AvatarText = "红",
                AvatarPath = "pack://application:,,,/Assets/Avatars/girl1.png",
                Time = "星期三"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "小兰", 
                LastMessage = "文档已经更新", 
                AvatarText = "兰",
                AvatarPath = "pack://application:,,,/Assets/Avatars/girl2.png",
                Time = "05/10"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "项目群", 
                LastMessage = "王五: 我已经提交了代码", 
                AvatarText = "群",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy5.png",
                Time = "05/08"
            });
            _chatList.Items.Add(new ChatItemViewModel { 
                Title = "游戏群", 
                LastMessage = "王五: 上号！", 
                AvatarText = "群",
                AvatarPath = "pack://application:,,,/Assets/Avatars/boy6.png",
                Time = "05/09"
            });
            _isInitialized = true;
        }
    }

    public partial class ChatListViewModel : ObservableObject
    {
        public ObservableCollection<ChatItemViewModel> Items { get; } = new();

        [ObservableProperty]
        private ChatItemViewModel _selectedItem;
    }

    public partial class ChatItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _lastMessage;

        [ObservableProperty]
        private string _avatarText;
        
        [ObservableProperty]
        private string _avatarPath;
        
        [ObservableProperty]
        private string _time;
    }

    public partial class ChatDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _inputText = string.Empty;

        public ObservableCollection<MessageViewModel> Messages { get; } = new();

        [RelayCommand]
        private void Send()
        {
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            // 添加新消息
            Messages.Add(new MessageViewModel
            {
                Content = InputText,
                IsFromMe = true
            });

            // 清空输入
            InputText = string.Empty;

            // 模拟回复
            Messages.Add(new MessageViewModel
            {
                Content = "这是自动回复的消息",
                IsFromMe = false
            });
        }
    }

    public partial class MessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _content;

        [ObservableProperty]
        private bool _isFromMe;
    }
}