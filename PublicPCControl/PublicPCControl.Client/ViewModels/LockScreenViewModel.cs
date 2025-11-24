// File: PublicPCControl.Client/ViewModels/LockScreenViewModel.cs
using System;
using System.Windows.Input;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.ViewModels
{
    public class LockScreenViewModel : ViewModelBase
    {
        private readonly Func<AppConfig> _getConfig;
        private readonly Action _navigateToLogin;
        private readonly Action _showAdmin;

        public ICommand StartCommand { get; }
        public ICommand AdminCommand { get; }

        public string Notice => "자료열람실 공용 PC입니다. 보안을 위해 활동이 기록됩니다.";
        public string AdminNotice => "관리자 전용 PC입니다. 일반 사용자는 사용할 수 없습니다.";

        public LockScreenViewModel(Func<AppConfig> getConfig, Action navigateToLogin, Action showAdmin)
        {
            _getConfig = getConfig;
            _navigateToLogin = navigateToLogin;
            _showAdmin = showAdmin;
            StartCommand = new RelayCommand(_ => _navigateToLogin(), _ => IsUserAllowed);
            AdminCommand = new RelayCommand(_ => _showAdmin());
        }

        public bool IsUserAllowed => !_getConfig().IsAdminOnlyPc && _getConfig().EnforcementEnabled;

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsUserAllowed));
            if (StartCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }
    }
}