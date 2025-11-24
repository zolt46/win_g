// File: PublicPCControl.Client/MainWindow.xaml.cs
using System.Windows;
using PublicPCControl.Client.ViewModels;

namespace PublicPCControl.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                DataContext = new MainViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "PublicPCControl", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}