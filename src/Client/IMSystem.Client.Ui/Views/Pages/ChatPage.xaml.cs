using System.Windows.Controls;
using IMSystem.Client.Ui.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.Views.Pages
{
    /// <summary>
    /// 聊天/消息页面
    /// </summary>
    public partial class ChatPage : INavigableView<ChatPageViewModel>
    {
        public ChatPageViewModel ViewModel { get; }

        public ChatPage(ChatPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}