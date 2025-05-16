using System.IO;
using System.Reflection;
using System.Windows.Threading;
using IMSystem.Client.Ui.Services;
using IMSystem.Client.Ui.ViewModels.Pages;
using IMSystem.Client.Ui.ViewModels.Windows;
using IMSystem.Client.Ui.Views.Pages;
using IMSystem.Client.Ui.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace IMSystem.Client.Ui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { 
                var basePath = Path.GetDirectoryName(AppContext.BaseDirectory) ?? 
                    throw new DirectoryNotFoundException("无法找到应用程序的基目录。");
                c.SetBasePath(basePath); 
            })
            .ConfigureServices((context, services) =>
            {
                // 添加页面提供程序
                services.AddNavigationViewPageProvider();

                // 创建ApplicationHostService实例
                // 同时注册为ApplicationHostService和IHostedService，确保是同一实例
                services.AddSingleton<ApplicationHostService>();
                services.AddHostedService(sp => sp.GetRequiredService<ApplicationHostService>());

                // 主题服务
                services.AddSingleton<IThemeService, ThemeService>();

                // 任务栏服务
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // 导航服务
                services.AddSingleton<INavigationService, NavigationService>();

                // 登录窗口
                services.AddSingleton<LoginWindow>();
                services.AddSingleton<LoginViewModel>();

                // 主窗口和导航
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // 页面和视图模型
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
            }).Build();

        /// <summary>
        /// 获取注册的服务
        /// </summary>
        /// <typeparam name="T">要获取的服务类型</typeparam>
        /// <returns>服务实例或 <see langword="null"/></returns>
        public static T? GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// 程序启动时触发
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        /// <summary>
        /// 程序退出时触发
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// 当应用程序抛出未处理异常时触发
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 更多信息请参见 https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
