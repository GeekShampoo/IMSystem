using IMSystem.Client.Ui.ViewModels.Windows;
using System;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace IMSystem.Client.Ui.Views.Windows
{
    public partial class LoginWindow : FluentWindow
    {
        public LoginViewModel ViewModel { get; }

        public LoginWindow(LoginViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            InitializeComponent();
        }

        #region IWindow methods

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion IWindow methods
    }
}