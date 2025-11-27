// File: PublicPCControl.Client/ViewModels/SessionViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;

namespace PublicPCControl.Client.ViewModels
{
    public class SessionViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly Action<string> _endSession;
        private readonly LoggingService _loggingService;
        private Session? _session;
        private readonly DispatcherTimer _timer;
        private TimeSpan _remaining;
        private bool _showWarning;
        private int _extensionsUsed;
        private int _maxExtensions;
        private int _extensionMinutes;

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();

        public Session? CurrentSession
        {
            get => _session;
            private set => SetProperty(ref _session, value);
        }

        public TimeSpan Remaining
        {
            get => _remaining;
            private set => SetProperty(ref _remaining, value);
        }

        public bool ShowWarning
        {
            get => _showWarning;
            set => SetProperty(ref _showWarning, value);
        }

        public int ExtensionsUsed
        {
            get => _extensionsUsed;
            private set => SetProperty(ref _extensionsUsed, value);
        }

        public int MaxExtensions
        {
            get => _maxExtensions;
            private set => SetProperty(ref _maxExtensions, value);
        }

        public int ExtensionMinutes
        {
            get => _extensionMinutes;
            private set => SetProperty(ref _extensionMinutes, value);
        }

        public int ExtensionsRemaining => Math.Max(0, MaxExtensions - ExtensionsUsed);

        public bool CanExtend => CurrentSession != null && ExtensionsRemaining > 0 && ExtensionMinutes > 0;

        public ICommand EndCommand { get; }
        public ICommand LaunchProgramCommand { get; }
        public ICommand ExtendCommand { get; }

        public SessionViewModel(SessionService sessionService, Action<string> endSession, LoggingService loggingService)
        {
            _sessionService = sessionService;
            _endSession = endSession;
            _loggingService = loggingService;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += TimerTick;
            EndCommand = new RelayCommand(_ => _endSession("manual"));
            LaunchProgramCommand = new RelayCommand(p => LaunchProgram(p as AllowedProgram), p => p is AllowedProgram);
            ExtendCommand = new RelayCommand(_ => ExtendSession(), _ => CanExtend);
        }

        public void BindSession(Session session, AppConfig config)
        {
            CurrentSession = session;
            Remaining = TimeSpan.FromMinutes(session.RequestedMinutes);
            ExtensionsUsed = session.ExtensionsUsed;
            MaxExtensions = session.MaxExtensions;
            ExtensionMinutes = session.ExtensionMinutes;
            AllowedPrograms.Clear();
            foreach (var program in config.AllowedPrograms)
            {
                AllowedPrograms.Add(program);
            }
            _timer.Start();
            RaiseExtensionStateChanged();
        }

        public void StopTimer() => _timer.Stop();

        public void ClearSession()
        {
            StopTimer();
            CurrentSession = null;
            AllowedPrograms.Clear();
            Remaining = TimeSpan.Zero;
            ShowWarning = false;
            ExtensionsUsed = 0;
            MaxExtensions = 0;
            ExtensionMinutes = 0;
            RaiseExtensionStateChanged();
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            if (CurrentSession == null) return;
            Remaining -= TimeSpan.FromSeconds(1);
            ShowWarning = Remaining.TotalMinutes <= 5;
            if (Remaining <= TimeSpan.Zero)
            {
                _timer.Stop();
                _endSession("timeout");
            }
        }

        private void ExtendSession()
        {
            if (_sessionService.TryExtendSession())
            {
                ExtensionsUsed = _sessionService.CurrentSession?.ExtensionsUsed ?? ExtensionsUsed;
                Remaining += TimeSpan.FromMinutes(ExtensionMinutes);
                RaiseExtensionStateChanged();
                ShowWarning = Remaining.TotalMinutes <= 5;
            }
        }

        private void RaiseExtensionStateChanged()
        {
            OnPropertyChanged(nameof(ExtensionsRemaining));
            OnPropertyChanged(nameof(CanExtend));
            if (ExtendCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        private void LaunchProgram(AllowedProgram? program)
        {
            if (program == null || CurrentSession == null) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = program.ExecutablePath,
                    Arguments = program.Arguments,
                    UseShellExecute = true
                });
                _loggingService.LogProcessStart(CurrentSession.Id, program.DisplayName, program.ExecutablePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}