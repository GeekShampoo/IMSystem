using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using IMSystem.Client.Ui.Views.Windows;
using Wpf.Ui;
using IMSystem.Client.Ui.Services;

namespace IMSystem.Client.Ui.ViewModels.Windows
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private bool _rememberMe = false;

        [ObservableProperty]
        private bool _autoLogin = false;

        [ObservableProperty]
        private bool _isRegistering = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        public LoginViewModel()
        {
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "请输入用户名";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "正在登录...";
                ErrorMessage = string.Empty;

                // TODO: 实际的登录逻辑
                await Task.Delay(1000); // 模拟网络请求
                
                // 登录成功，使用ApplicationHostService打开主窗口
                var hostService = App.GetService<ApplicationHostService>();
                if (hostService != null)
                {
                    hostService.OpenMainWindow();
                    
                    // 关闭当前登录窗口
                    var currentWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
                    currentWindow?.Close();
                }
                else
                {
                    throw new InvalidOperationException("无法获取ApplicationHostService");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"登录失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void SwitchToRegister()
        {
            IsRegistering = true;
            ClearFields();
        }

        [RelayCommand]
        private void SwitchToLogin()
        {
            IsRegistering = false;
            ClearFields();
        }

        [RelayCommand]
        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "请输入用户名";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                return;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "请确认密码";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "两次输入的密码不一致";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "请输入邮箱地址";
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "正在注册...";
                ErrorMessage = string.Empty;

                // TODO: 实际的注册逻辑
                await Task.Delay(1000); // 模拟网络请求

                // 注册成功后切换到登录界面
                IsRegistering = false;
                StatusMessage = "注册成功，请登录";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"注册失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void ForgotPassword()
        {
            // TODO: 忘记密码逻辑
            StatusMessage = "请联系管理员重置密码";
        }

        private void ClearFields()
        {
            Username = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            Email = string.Empty;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
        }
    }
}