// File: PublicPCControl.Client/App.xaml.cs
using System;
using System.IO;
using System.Windows;
using PublicPCControl.Client.ViewModels;

namespace PublicPCControl.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
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
    }
}