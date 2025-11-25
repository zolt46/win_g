// File: PublicPCControl.Client/Views/AdminPasswordWindow.xaml.cs
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class AdminPasswordWindow : Window
    {
        public string Password { get; private set; } = string.Empty;

        public AdminPasswordWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => PasswordInput.Focus();
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            Password = PasswordInput.Password;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}