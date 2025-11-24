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
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "PublicPCControl", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}