// File: PublicPCControl.Client/ViewModels/UserLoginViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.ViewModels
{
    public class UserLoginViewModel : ViewModelBase
    {
        public record LoginRequest(string UserName, string UserId, string Purpose, int RequestedMinutes);

        private readonly Func<AppConfig> _getConfig;
        private readonly Action<LoginRequest> _startSession;
        private readonly Action _cancel;

        private string _userName = string.Empty;
        private string _userId = string.Empty;
        private string _purpose = "자료검색";
        private int _requestedMinutes;
        private bool _consent;

        public ObservableCollection<string> PurposeOptions { get; } = new(new[] { "자료검색", "문서작성", "기타" });

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public string Purpose
        {
            get => _purpose;
            set => SetProperty(ref _purpose, value);
        }

        public int RequestedMinutes
        {
            get => _requestedMinutes;
            set => SetProperty(ref _requestedMinutes, value);
        }

        public bool Consent
        {
            get => _consent;
            set => SetProperty(ref _consent, value);
        }

        public ICommand StartCommand { get; }
        public ICommand CancelCommand { get; }

        public UserLoginViewModel(Func<AppConfig> getConfig, Action<LoginRequest> startSession, Action cancel)
        {
            _getConfig = getConfig;
            _startSession = startSession;
            _cancel = cancel;
            _requestedMinutes = _getConfig().DefaultSessionMinutes;
            StartCommand = new RelayCommand(_ => ExecuteStart(), _ => CanStart());
            CancelCommand = new RelayCommand(_ => _cancel());
        }

        private bool CanStart() => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(UserId) && Consent;

        private void ExecuteStart()
        {
            var minutes = Math.Clamp(RequestedMinutes, 5, _getConfig().MaxSessionMinutes);
            _startSession(new LoginRequest(UserName, UserId, Purpose, minutes));
            Consent = false;
            UserName = string.Empty;
            UserId = string.Empty;
            RequestedMinutes = _getConfig().DefaultSessionMinutes;
        }
    }
}