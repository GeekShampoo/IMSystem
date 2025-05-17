using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IMSystem.Client.Ui.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.Views.Pages
{
    /// <summary>
    /// 联系人页面
    /// </summary>
    public partial class ContactsPage : INavigableView<ContactsPageViewModel>
    {
        public ContactsPageViewModel ViewModel { get; }

        public ContactsPage(ContactsPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
        
        /// <summary>
        /// 好友项选中事件处理
        /// </summary>
        private void FriendItem_Selected(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ContactItemViewModel contact)
            {
                ViewModel.ContactList.SelectedItem = contact;
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 群组项选中事件处理
        /// </summary>
        private void GroupItem_Selected(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ContactItemViewModel contact)
            {
                ViewModel.ContactList.SelectedItem = contact;
                e.Handled = true;
            }
        }
    }
}