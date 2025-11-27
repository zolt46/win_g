// File: PublicPCControl.Client/ViewModels/LockScreenViewModel.cs
using System;
using System.Windows.Threading;
using System.Windows.Input;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.ViewModels
{
    public class LockScreenViewModel : ViewModelBase
    {
        private readonly Func<AppConfig> _getConfig;
        private readonly Func<bool> _getMaintenanceState;
        private readonly Action _navigateToLogin;
        private readonly Action _requestAdmin;
        private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private DateTime _currentTime = DateTime.Now;

        public ICommand StartCommand { get; }
        public ICommand AdminCommand { get; }

        public string Notice => "자료열람실 공용 PC입니다. 보안을 위해 활동이 기록됩니다.";
        public string ModeLabel =>
            _getMaintenanceState()
                ? "관리자 모드 (운영 일시 중지)"
                : IsUserAllowed ? "일반 이용" : "관리자 전용";

        public DateTime CurrentTime
        {
            get => _currentTime;
            private set => SetProperty(ref _currentTime, value);
        }

        public LockScreenViewModel(Func<AppConfig> getConfig, Func<bool> getMaintenanceState, Action navigateToLogin, Action requestAdmin)
        {
            _getConfig = getConfig;
            _getMaintenanceState = getMaintenanceState;
            _navigateToLogin = navigateToLogin;
            _requestAdmin = requestAdmin;
            StartCommand = new RelayCommand(_ => _navigateToLogin(), _ => IsUserAllowed);
            AdminCommand = new RelayCommand(_ => _requestAdmin());
            _clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now;
            _clockTimer.Start();
        }

        public bool IsUserAllowed => !_getConfig().IsAdminOnlyPc && !_getMaintenanceState();

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsUserAllowed));
            OnPropertyChanged(nameof(ModeLabel));
            OnPropertyChanged(nameof(CurrentTime));
            if (StartCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }
    }
}