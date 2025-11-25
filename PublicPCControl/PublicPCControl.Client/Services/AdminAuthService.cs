// File: PublicPCControl.Client/Services/AdminAuthService.cs
using System;
using System.Windows;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Views;

namespace PublicPCControl.Client.Services
{
    public class AdminAuthService
    {
        public bool EnsureAuthenticated(Window owner, AppConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.AdminPasswordHash))
            {
                MessageBox.Show(owner,
                    "관리자 비밀번호가 설정되어 있지 않습니다. 관리자 화면에서 비밀번호를 먼저 설정해 주세요.",
                    "관리자 인증 필요",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var dialog = new AdminPasswordWindow
            {
                Owner = owner
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                return false;
            }

            var inputHash = ConfigService.HashPassword(dialog.Password);
            if (string.Equals(inputHash, config.AdminPasswordHash, StringComparison.Ordinal))
            {
                return true;
            }

            MessageBox.Show(owner,
                "비밀번호가 올바르지 않습니다.",
                "인증 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }
}