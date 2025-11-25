// File: PublicPCControl.Client/Services/AdminAuthService.cs
using System;
using System.Windows;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Views;

namespace PublicPCControl.Client.Services
{
    public class AdminAuthService
    {
        private readonly ConfigService _configService;

        public AdminAuthService(ConfigService configService)
        {
            _configService = configService;
        }

        public bool EnsureAuthenticated(Window owner, AppConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.AdminPasswordHash))
            {
                var setupDialog = new AdminPasswordSetupWindow
                {
                    Owner = owner
                };

                var setupResult = setupDialog.ShowDialog();
                if (setupResult != true || string.IsNullOrWhiteSpace(setupDialog.Password))
                {
                    return false;
                }

                config.AdminPasswordHash = ConfigService.HashPassword(setupDialog.Password);
                _configService.Save(config);

                MessageBox.Show(owner,
                    "새 관리자 비밀번호가 설정되었습니다.",
                    "관리자 비밀번호 설정 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return true;
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