using IMSystem.Client.Ui.Views.Pages;
using IMSystem.Client.Ui.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Wpf.Ui;

namespace IMSystem.Client.Ui.Services
{
    /// <summary>
    /// 应用程序托管服务
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private INavigationWindow? _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 当应用程序主机准备启动服务时触发
        /// </summary>
        /// <param name="cancellationToken">指示启动过程已中止</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// 当应用程序主机执行优雅关闭时触发
        /// </summary>
        /// <param name="cancellationToken">指示关闭过程不再优雅</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 在激活过程中创建窗口
        /// </summary>
        private async Task HandleActivationAsync()
        {
            // 检查是否有已经打开的登录窗口
            if (!Application.Current.Windows.OfType<LoginWindow>().Any())
            {
                // 首先显示登录窗口
                var loginWindow = _serviceProvider.GetService<LoginWindow>();
                loginWindow?.ShowWindow();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 在用户成功登录后打开主窗口
        /// </summary>
        public void OpenMainWindow()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (_serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;
                
                if (_navigationWindow != null)
                {
                    _navigationWindow.ShowWindow();
                    _navigationWindow.Navigate(typeof(Views.Pages.DashboardPage));
                }
            }
        }
    }
}
