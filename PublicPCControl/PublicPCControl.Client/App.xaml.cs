// File: PublicPCControl.Client/App.xaml.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using PublicPCControl.Client.Services;
using PublicPCControl.Client.ViewModels;

namespace PublicPCControl.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                var configService = new ConfigService();
                var authService = new AdminAuthService(configService);

                MainWindow? mainWindow = null;
                MainViewModel viewModel = null!;
                viewModel = new MainViewModel(
                    configService,
                    () => authService.EnsureAuthenticated(mainWindow!, viewModel.Config));

                mainWindow = new MainWindow(viewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "startup_error.log");

                File.WriteAllText(logPath, ex.ToString());

                // 마지막에 한번 더 알려주기 (여기서 나는 소리가 경고음일 수도 있음)
                MessageBox.Show(
                    "시작 중 오류가 발생했습니다.\n\n" +
                    "startup_error.log 파일을 관리자에게 전달해 주세요.",
                    "시작 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown(-1);
            }
        }

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorReporter.Log("Dispatcher", e.Exception);
            MessageBox.Show(
                "예상치 못한 오류가 발생했습니다.\n계속하려면 확인을 눌러주세요.\n\nerror.log 파일을 관리자에게 전달해 주세요.",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ErrorReporter.Log("AppDomain", ex);
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ErrorReporter.Log("Task", e.Exception);
            e.SetObserved();
        }
    }
}