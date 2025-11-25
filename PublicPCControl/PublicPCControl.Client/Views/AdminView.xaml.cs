// File: PublicPCControl.Client/Views/AdminView.xaml.cs
using System.Windows.Controls;
using PublicPCControl.Client.ViewModels;

namespace PublicPCControl.Client.Views
{
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not AdminViewModel vm || sender is not PasswordBox passwordBox)
            {
                return;
            }

            if ((passwordBox.Tag as string) == "Primary")
            {
                vm.NewAdminPassword = passwordBox.Password;
            }
            else if ((passwordBox.Tag as string) == "Confirm")
            {
                vm.ConfirmAdminPassword = passwordBox.Password;
            }
        }
    }
}