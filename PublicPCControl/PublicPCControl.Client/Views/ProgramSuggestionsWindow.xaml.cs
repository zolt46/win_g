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

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
