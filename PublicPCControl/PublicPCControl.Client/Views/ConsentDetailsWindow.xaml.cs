// File: PublicPCControl.Client/Views/ConsentDetailsWindow.xaml.cs
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class ConsentDetailsWindow : Window
    {
        public ConsentDetailsWindow()
        {
            InitializeComponent();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}