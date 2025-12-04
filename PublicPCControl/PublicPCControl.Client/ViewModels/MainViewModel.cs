// File: PublicPCControl.Client/ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using PublicPCControl.Client.Data;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;
using Views = PublicPCControl.Client.Views;

namespace PublicPCControl.Client.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly SessionService _sessionService;
        private readonly LoggingService _loggingService;
        private readonly ProcessMonitorService _processMonitor;
        private readonly WindowMonitorService _windowMonitor;
        private readonly Func<bool> _authorizeAdmin;
        private Views.AdminMaintenanceWindow? _maintenanceWindow;
        private bool _adminMaintenanceActive;

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

        public MainViewModel(ConfigService? configService = null, Func<bool>? authorizeAdmin = null)
        {
            DatabaseInitializer.EnsureDatabase();
            _configService = configService ?? new ConfigService();
            _authorizeAdmin = authorizeAdmin ?? (() => true);
            _config = _configService.Load();

            var connectionString = DatabaseInitializer.GetConnectionString();
            var sessionRepository = new SessionRepository(connectionString);
            var logRepository = new LogRepository(connectionString);
            _sessionService = new SessionService(sessionRepository);
            var auditLogWriter = new AuditLogWriter();
            _loggingService = new LoggingService(logRepository, auditLogWriter);
            _processMonitor = new ProcessMonitorService(_loggingService, () => Config, () => _sessionService.CurrentSession);
            _windowMonitor = new WindowMonitorService(OnWindowChanged);

            LockScreenViewModel = new LockScreenViewModel(() => Config, () => _adminMaintenanceActive, NavigateToLogin, RequestAdminView);
            UserLoginViewModel = new UserLoginViewModel(() => Config, StartSessionFromLogin, NavigateToLockScreen);
            SessionViewModel = new SessionViewModel(_sessionService, EndSessionFromView, _loggingService);
            AdminViewModel = new AdminViewModel(_configService, UpdateConfig, NavigateToLockScreen, EnterAdminMaintenanceMode, ResumeFromAdminMaintenance, () => _adminMaintenanceActive);

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
            LockScreenViewModel.Refresh();
        }

        private void ShowAdminView()
        {
            AdminViewModel.Refresh(Config);
            CurrentViewModel = AdminViewModel;
        }

        private void RequestAdminView()
        {
            if (!_authorizeAdmin())
            {
                return;
            }

            try
            {
                ShowAdminView();
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Log("AdminView", ex);
                System.Windows.MessageBox.Show(
                    "관리자 화면을 여는 중 오류가 발생했습니다.\n잠시 후 다시 시도하거나 error.log를 확인해 주세요.",
                    "관리자 화면 오류",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public void HandleAdminShortcut()
        {
            RequestAdminView();
        }

        private void StartSessionFromLogin(UserLoginViewModel.LoginRequest request)
        {
            try
            {
                var config = Config;
                var session = _sessionService.StartSession(
                    request.UserName,
                    request.UserId,
                    request.Purpose,
                    config.DefaultSessionMinutes,
                    config.AllowExtensions ? config.MaxExtensionCount : 0,
                    config.AllowExtensions ? config.SessionExtensionMinutes : 0);
                SessionViewModel.BindSession(session, Config);
                CurrentViewModel = SessionViewModel;
                if (Config.EnforcementEnabled)
                {
                    _processMonitor.Start();
                    _windowMonitor.Start();
                }
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Log("StartSession", ex);
                System.Windows.MessageBox.Show(
                    "세션을 시작하는 중 오류가 발생했습니다.\n입력 정보를 확인 후 다시 시도하거나 error.log를 관리자에게 전달해 주세요.",
                    "세션 시작 오류",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void EndSessionFromView(string reason)
        {
            _processMonitor.Stop();
            _windowMonitor.Stop();
            _sessionService.EndSession(reason);
            SessionViewModel.ClearSession();
            NavigateToLockScreen();
        }

        private void EnterAdminMaintenanceMode()
        {
            if (_adminMaintenanceActive)
            {
                return;
            }

            _adminMaintenanceActive = true;
            _processMonitor.Stop();
            _windowMonitor.Stop();
            LockScreenViewModel.Refresh();
            AdminViewModel.NotifyMaintenanceStateChanged();

            var mainWindow = System.Windows.Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Hide();
            }

            _maintenanceWindow = new Views.AdminMaintenanceWindow
            {
                Owner = mainWindow
            };
            _maintenanceWindow.ResumeRequested += MaintenanceWindowOnResumeRequested;
            _maintenanceWindow.Closed += MaintenanceWindowOnClosed;
            _maintenanceWindow.Show();
        }

        private void ResumeFromAdminMaintenance()
        {
            if (!_adminMaintenanceActive)
            {
                return;
            }

            _adminMaintenanceActive = false;
            LockScreenViewModel.Refresh();
            AdminViewModel.NotifyMaintenanceStateChanged();
            var maintenanceWindow = _maintenanceWindow;
            _maintenanceWindow = null;
            if (maintenanceWindow != null)
            {
                maintenanceWindow.ResumeRequested -= MaintenanceWindowOnResumeRequested;
                maintenanceWindow.Closed -= MaintenanceWindowOnClosed;
                if (maintenanceWindow.IsVisible)
                {
                    maintenanceWindow.Close();
                }
            }

            var mainWindow = System.Windows.Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.Activate();
            }

            if (_sessionService.CurrentSession != null && Config.EnforcementEnabled)
            {
                _processMonitor.Start();
                _windowMonitor.Start();
            }

            CurrentViewModel = LockScreenViewModel;
        }

        private void MaintenanceWindowOnResumeRequested(object? sender, System.EventArgs e)
        {
            ResumeFromAdminMaintenance();
        }

        private void MaintenanceWindowOnClosed(object? sender, System.EventArgs e)
        {
            ResumeFromAdminMaintenance();
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
            LockScreenViewModel.Refresh();
            UserLoginViewModel.RefreshConfig();
        }
    }
}