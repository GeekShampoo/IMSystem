using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Abstractions.Controls;

namespace IMSystem.Client.Ui.ViewModels.Pages
{
    public partial class ContactsPageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ContactListViewModel _contactList;

        [ObservableProperty]
        private ContactDetailViewModel _contactDetail;

        public ContactsPageViewModel()
        {
            // 构造函数仅初始化必要的对象，详细初始化移至InitializeViewModel
            _contactList = new ContactListViewModel();
            _contactDetail = new ContactDetailViewModel();
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
            // 监听联系人列表选择变化
            _contactList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ContactListViewModel.SelectedItem) && _contactList.SelectedItem != null)
                {
                    // 更新联系人详情
                    UpdateContactDetail(_contactList.SelectedItem);
                }
            };

            // 监听标签页变化，更新列表内容
            _contactList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ContactListViewModel.SelectedTabIndex))
                {
                    UpdateCurrentItems();
                }
            };

            // 初始化模拟数据
            InitializeMockData();

            _isInitialized = true;
        }

        private void InitializeMockData()
        {
            // 添加好友数据
            _contactList.Friends.Add(new ContactItemViewModel
            {
                DisplayName = "张三",
                AvatarText = "张",
                AccountId = "10001",
                IsGroup = false
            });
            _contactList.Friends.Add(new ContactItemViewModel
            {
                DisplayName = "李四",
                AvatarText = "李",
                AccountId = "10002",
                IsGroup = false
            });
            _contactList.Friends.Add(new ContactItemViewModel
            {
                DisplayName = "王五",
                AvatarText = "王",
                AccountId = "10003",
                IsGroup = false
            });

            // 添加群组数据
            _contactList.Groups.Add(new ContactItemViewModel
            {
                DisplayName = "项目开发群",
                AvatarText = "项",
                AccountId = "20001",
                IsGroup = true,
                MemberCount = 8
            });
            _contactList.Groups.Add(new ContactItemViewModel
            {
                DisplayName = "技术交流群",
                AvatarText = "技",
                AccountId = "20002",
                IsGroup = true,
                MemberCount = 120
            });

            // 更新当前显示的列表
            UpdateCurrentItems();

            // 默认选中第一项
            if (_contactList.CurrentItems.Count > 0)
            {
                _contactList.SelectedItem = _contactList.CurrentItems[0];
            }
        }

        private void UpdateCurrentItems()
        {
            _contactList.CurrentItems.Clear();
            
            foreach (var item in _contactList.SelectedTabIndex == 0 ? _contactList.Friends : _contactList.Groups)
            {
                _contactList.CurrentItems.Add(item);
            }
        }

        private void UpdateContactDetail(ContactItemViewModel contact)
        {
            _contactDetail.DisplayName = contact.DisplayName;
            _contactDetail.AvatarText = contact.AvatarText;
            _contactDetail.AccountId = contact.AccountId;
            _contactDetail.IsGroup = contact.IsGroup;
            _contactDetail.MemberCount = contact.MemberCount;
            
            if (contact.IsGroup)
            {
                _contactDetail.StatusText = $"{contact.MemberCount}个成员";
                _contactDetail.Description = "这是一个群组的简介信息，用于展示群组的基本情况和说明。";
                _contactDetail.Remark = "未设置备注";
            }
            else
            {
                _contactDetail.StatusText = "在线";
                _contactDetail.Description = "这是一段个人简介，用户可以在此处编辑自己的个性化签名。";
                _contactDetail.Remark = contact.DisplayName == "张三" ? "同事" : "未设置备注";
            }
        }
    }

    public partial class ContactListViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _selectedTabIndex = 0; // 0: 好友, 1: 群组

        [ObservableProperty]
        private ContactItemViewModel _selectedItem;

        public ObservableCollection<ContactItemViewModel> Friends { get; } = new();
        public ObservableCollection<ContactItemViewModel> Groups { get; } = new();
        public ObservableCollection<ContactItemViewModel> CurrentItems { get; } = new();
    }

    public partial class ContactItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _avatarText;

        [ObservableProperty]
        private string _accountId;

        [ObservableProperty]
        private bool _isGroup;

        [ObservableProperty]
        private int _memberCount;
    }

    public partial class ContactDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _avatarText;

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private string _accountId;

        [ObservableProperty]
        private string _remark;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private bool _isGroup;

        [ObservableProperty]
        private int _memberCount;
    }
}