// File: PublicPCControl.Client/ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using PublicPCControl.Client.Data;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;

namespace PublicPCControl.Client.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly SessionService _sessionService;
        private readonly LoggingService _loggingService;
        private readonly ProcessMonitorService _processMonitor;
        private readonly WindowMonitorService _windowMonitor;

        private AppConfig _config;
        private ViewModelBase? _currentViewModel;

        public AppConfig Config
        {
            get => _config;
            set => SetProperty(ref _config, value);
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public LockScreenViewModel LockScreenViewModel { get; }
        public UserLoginViewModel UserLoginViewModel { get; }
        public SessionViewModel SessionViewModel { get; }
        public AdminViewModel AdminViewModel { get; }

        public MainViewModel()
        {
            DatabaseInitializer.EnsureDatabase();
            _configService = new ConfigService();
            _config = _configService.Load();

            var connectionString = DatabaseInitializer.GetConnectionString();
            var sessionRepository = new SessionRepository(connectionString);
            var logRepository = new LogRepository(connectionString);
            _sessionService = new SessionService(sessionRepository);
            _loggingService = new LoggingService(logRepository);
            _processMonitor = new ProcessMonitorService(_loggingService, () => Config, () => _sessionService.CurrentSession);
            _windowMonitor = new WindowMonitorService(OnWindowChanged);

            LockScreenViewModel = new LockScreenViewModel(() => Config, NavigateToLogin, ShowAdminView);
            UserLoginViewModel = new UserLoginViewModel(() => Config, StartSessionFromLogin, NavigateToLockScreen);
            SessionViewModel = new SessionViewModel(_sessionService, EndSessionFromView, _loggingService);
            AdminViewModel = new AdminViewModel(_configService, UpdateConfig, NavigateToLockScreen);

            CurrentViewModel = LockScreenViewModel;
        }

        private void NavigateToLogin()
        {
            CurrentViewModel = UserLoginViewModel;
        }

        private void NavigateToLockScreen()
        {
            SessionViewModel.StopTimer();
            CurrentViewModel = LockScreenViewModel;
        }

        private void ShowAdminView()
        {
            AdminViewModel.Refresh(Config);
            CurrentViewModel = AdminViewModel;
        }

        private void StartSessionFromLogin(UserLoginViewModel.LoginRequest request)
        {
            var session = _sessionService.StartSession(request.UserName, request.UserId, request.Purpose, request.RequestedMinutes);
            SessionViewModel.BindSession(session, Config);
            CurrentViewModel = SessionViewModel;
            if (Config.EnforcementEnabled)
            {
                _processMonitor.Start();
                _windowMonitor.Start();
            }
        }

        private void EndSessionFromView(string reason)
        {
            _processMonitor.Stop();
            _windowMonitor.Stop();
            _sessionService.EndSession(reason);
            NavigateToLockScreen();
        }

        private void OnWindowChanged(string process, string title)
        {
            var session = _sessionService.CurrentSession;
            if (session != null && Config.EnforcementEnabled)
            {
                _loggingService.LogWindowChange(session.Id, process, title);
            }
        }

        private void UpdateConfig(AppConfig config)
        {
            Config = config;
            _configService.Save(config);
        }
    }
}