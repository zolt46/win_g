// File: PublicPCControl.Client/Views/UserLoginView.xaml.cs
using System.Windows.Controls;
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class UserLoginView : UserControl
    {
        public UserLoginView()
        {
            InitializeComponent();
        }

        private void OnShowConsentDetails(object sender, RoutedEventArgs e)
        {
            var dialog = new ConsentDetailsWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && DataContext is ViewModels.UserLoginViewModel vm)
            {
                vm.Consent = true;
            }
        }
    }
}