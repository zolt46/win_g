// File: PublicPCControl.Client/Views/AdminView.xaml.cs
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PublicPCControl.Client.ViewModels;

namespace PublicPCControl.Client.Views
{
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();
        }

        private void OnBrowseProgram(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminViewModel vm)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "실행 파일 (*.exe)|*.exe|모든 파일 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                vm.NewProgramPath = dialog.FileName;
                if (string.IsNullOrWhiteSpace(vm.NewProgramName))
                {
                    vm.NewProgramName = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }

        private void OnChangePassword(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminViewModel vm)
            {
                return;
            }

            var dialog = new AdminPasswordChangeWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewPassword))
            {
                vm.ApplyNewAdminPassword(dialog.NewPassword);
                MessageBox.Show("관리자 비밀번호가 변경되었습니다.", "비밀번호 변경", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
