// File: PublicPCControl.Client/Views/AdminPasswordChangeWindow.xaml.cs
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class AdminPasswordChangeWindow : Window
    {
        public string NewPassword { get; private set; } = string.Empty;

        public AdminPasswordChangeWindow()
        {
            InitializeComponent();
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Password;
            var confirm = ConfirmPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                ErrorMessage.Text = "비밀번호는 최소 4자 이상 입력하세요.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!string.Equals(password, confirm))
            {
                ErrorMessage.Text = "비밀번호와 확인 값이 일치하지 않습니다.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            NewPassword = password;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
