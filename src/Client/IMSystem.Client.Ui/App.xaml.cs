using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using IMSystem.Client.Ui.Services;
using IMSystem.Client.Ui.ViewModels.Pages;
using IMSystem.Client.Ui.ViewModels.Windows;
using IMSystem.Client.Ui.Views.Pages;
using IMSystem.Client.Ui.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.DependencyInjection;

namespace IMSystem.Client.Ui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // 应用Host
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();
                // 注册主窗口
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
;
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // 注册页面
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                // 注册IMSystem自定义页面
                services.AddSingleton<ChatPage>();
                services.AddSingleton<ChatPageViewModel>();
                services.AddSingleton<ContactsPage>();
                services.AddSingleton<ContactsPageViewModel>();
                services.AddSingleton<FavoritesPage>();
                services.AddSingleton<FavoritesPageViewModel>();
                services.AddSingleton<FilesPage>();
                services.AddSingleton<FilesPageViewModel>();
                
            })
            .Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>() where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.Start();

            var mainWindow = GetService<MainWindow>();
            mainWindow.Loaded += OnMainWindowLoaded;
            mainWindow.ShowWindow();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private void OnExit(object sender, ExitEventArgs e)
        {
            _host.StopAsync().Wait();
            _host.Dispose();
        }

        /// <summary>
        /// Occurs when the main window finishes loading.
        /// </summary>
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            var navigationService = GetService<INavigationService>();
            navigationService.Navigate(typeof(ChatPage));
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
