using System.Windows.Controls;
using IMSystem.Client.Ui.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.Views.Pages
{
    /// <summary>
    /// 收藏页面
    /// </summary>
    public partial class FavoritesPage : INavigableView<FavoritesPageViewModel>
    {
        public FavoritesPageViewModel ViewModel { get; }

        public FavoritesPage(FavoritesPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}