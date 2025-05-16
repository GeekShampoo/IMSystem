using System.Windows.Controls;
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
    }
}