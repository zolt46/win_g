// File: PublicPCControl.Client/App.xaml.cs
using System;
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
                MessageBox.Show($"앱 초기화 중 오류가 발생했습니다: {ex.Message}", "초기화 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}