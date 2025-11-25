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
        private readonly Action _requestAdmin;

        public ICommand StartCommand { get; }
        public ICommand AdminCommand { get; }

        public string Notice => "자료열람실 공용 PC입니다. 보안을 위해 활동이 기록됩니다.";
        public string ModeLabel => IsUserAllowed ? "일반 이용" : "관리자 전용";

        public LockScreenViewModel(Func<AppConfig> getConfig, Action navigateToLogin, Action requestAdmin)
        {
            _getConfig = getConfig;
            _navigateToLogin = navigateToLogin;
            _requestAdmin = requestAdmin;
            StartCommand = new RelayCommand(_ => _navigateToLogin(), _ => IsUserAllowed);
            AdminCommand = new RelayCommand(_ => _requestAdmin());
        }

        public bool IsUserAllowed => !_getConfig().IsAdminOnlyPc;

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsUserAllowed));
            OnPropertyChanged(nameof(ModeLabel));
            if (StartCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }
    }
}