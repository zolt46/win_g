// File: PublicPCControl.Client/Views/AdminPasswordSetupWindow.xaml.cs
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class AdminPasswordSetupWindow : Window
    {
        public string Password { get; private set; } = string.Empty;

        public AdminPasswordSetupWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => PasswordBox.Focus();
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Password;
            var confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                ShowError("비밀번호는 4자 이상 입력해 주세요.");
                return;
            }

            if (!string.Equals(password, confirm))
            {
                ShowError("비밀번호와 확인 값이 일치하지 않습니다.");
                return;
            }

            Password = password;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}