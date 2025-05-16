using System.Windows.Controls;
using IMSystem.Client.Ui.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.Views.Pages
{
    /// <summary>
    /// 文件页面
    /// </summary>
    public partial class FilesPage : INavigableView<FilesPageViewModel>
    {
        public FilesPageViewModel ViewModel { get; }

        public FilesPage(FilesPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}