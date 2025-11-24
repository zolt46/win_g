// File: PublicPCControl.Client/Views/LockScreenView.xaml.cs
using System.Windows.Controls;
using System.Windows.Input;

namespace PublicPCControl.Client.Views
{
    public partial class LockScreenView : UserControl
    {
        public LockScreenView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Keyboard.Focus(this);
        }
    }
}