// File: PublicPCControl.Client/Views/AdminMaintenanceWindow.xaml.cs
using System;
using System.Windows;

namespace PublicPCControl.Client.Views
{
    public partial class AdminMaintenanceWindow : Window
    {
        public event EventHandler? ResumeRequested;

        public AdminMaintenanceWindow()
        {
            InitializeComponent();
        }

        private void OnResume(object sender, RoutedEventArgs e)
        {
            ResumeRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
