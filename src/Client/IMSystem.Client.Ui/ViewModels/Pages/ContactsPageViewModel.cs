using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.ViewModels.Pages
{
    public partial class ContactsPageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ContactListViewModel _contactList;

        [ObservableProperty]
        private ContactDetailViewModel _contactDetail;

        [ObservableProperty]
        private bool _hasSelectedContact = false;

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
                if (e.PropertyName == nameof(ContactListViewModel.SelectedItem))
                {
                    if (_contactList.SelectedItem != null)
                    {
                        // 更新联系人详情
                        UpdateContactDetail(_contactList.SelectedItem);
                        // 更新选中状态
                        HasSelectedContact = true;
                    }
                    else
                    {
                        // 没有选中项，显示欢迎界面
                        HasSelectedContact = false;
                    }
                }
            };
            
            // 监听选项卡索引变化
            _contactList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ContactListViewModel.SelectedTabIndex))
                {
                    // 清除当前选中项，避免跨列表选择问题
                    _contactList.SelectedItem = null;
                    
                    // 重置选中项
                    if (_contactList.SelectedTabIndex == 0 && _contactList.FriendGroups.Count > 0 && 
                        _contactList.FriendGroups[0].Items.Count > 0)
                    {
                        _contactList.SelectedItem = _contactList.FriendGroups[0].Items[0];
                    }
                    else if (_contactList.SelectedTabIndex == 1 && _contactList.GroupGroups.Count > 0 && 
                             _contactList.GroupGroups[0].Items.Count > 0)
                    {
                        _contactList.SelectedItem = _contactList.GroupGroups[0].Items[0];
                    }
                }
            };

            // 初始化模拟数据
            InitializeMockData();

            _isInitialized = true;
        }

        private void InitializeMockData()
        {
            // 添加好友分组
            var myFriendsGroup = new ContactGroupViewModel { GroupName = "我的好友" };
            var colleaguesGroup = new ContactGroupViewModel { GroupName = "同事" };
            var classmates = new ContactGroupViewModel { GroupName = "同学" };
            
            // 添加好友数据到分组
            myFriendsGroup.Items.Add(new ContactItemViewModel
            {
                DisplayName = "张三",
                AvatarText = "张",
                AccountId = "10001",
                IsGroup = false
            });
            
            colleaguesGroup.Items.Add(new ContactItemViewModel
            {
                DisplayName = "李四",
                AvatarText = "李",
                AccountId = "10002",
                IsGroup = false
            });
            
            classmates.Items.Add(new ContactItemViewModel
            {
                DisplayName = "王五",
                AvatarText = "王",
                AccountId = "10003",
                IsGroup = false
            });
            
            colleaguesGroup.Items.Add(new ContactItemViewModel
            {
                DisplayName = "赵六",
                AvatarText = "赵",
                AccountId = "10004",
                IsGroup = false
            });
            
            // 将分组添加到FriendGroups集合
            _contactList.FriendGroups.Add(myFriendsGroup);
            _contactList.FriendGroups.Add(colleaguesGroup);
            _contactList.FriendGroups.Add(classmates);

            // 添加群组分组
            var workGroupsCategory = new ContactGroupViewModel { GroupName = "工作群" };
            var interestGroupsCategory = new ContactGroupViewModel { GroupName = "兴趣群" };
            
            // 添加群组数据到分组
            workGroupsCategory.Items.Add(new ContactItemViewModel
            {
                DisplayName = "项目开发群",
                AvatarText = "项",
                AccountId = "20001",
                IsGroup = true,
                MemberCount = 8
            });
            
            interestGroupsCategory.Items.Add(new ContactItemViewModel
            {
                DisplayName = "技术交流群",
                AvatarText = "技",
                AccountId = "20002",
                IsGroup = true,
                MemberCount = 120
            });
            
            workGroupsCategory.Items.Add(new ContactItemViewModel
            {
                DisplayName = "产品设计群",
                AvatarText = "产",
                AccountId = "20003",
                IsGroup = true,
                MemberCount = 15
            });
            
            // 将分组添加到GroupGroups集合
            _contactList.GroupGroups.Add(workGroupsCategory);
            _contactList.GroupGroups.Add(interestGroupsCategory);

            // 默认选中第一个好友
            if (_contactList.FriendGroups.Count > 0 && _contactList.FriendGroups[0].Items.Count > 0)
            {
                _contactList.SelectedItem = _contactList.FriendGroups[0].Items[0];
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

        // 弃用原先的Friends和Groups集合，改用分组结构
        public ObservableCollection<ContactGroupViewModel> FriendGroups { get; } = new();
        public ObservableCollection<ContactGroupViewModel> GroupGroups { get; } = new();
    }

    // 新增分组视图模型
    public partial class ContactGroupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _groupName;
        
        [ObservableProperty]
        private bool _isExpanded = false;  // 修改默认值为 false，使分组默认折叠
        
        public ObservableCollection<ContactItemViewModel> Items { get; } = new();
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