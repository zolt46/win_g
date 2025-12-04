// File: PublicPCControl.Client/Views/ProgramSuggestionsWindow.xaml.cs
using System;
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class ProgramSuggestionsWindow : Window
    {
        public ProgramSuggestionsWindow()
        {
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            var resourceLocater = new Uri("/PublicPCControl.Client;component/Views/ProgramSuggestionsWindow.xaml", UriKind.Relative);
            Application.LoadComponent(this, resourceLocater);
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
