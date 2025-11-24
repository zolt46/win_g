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
            DataContext = new MainViewModel();
        }
    }
}