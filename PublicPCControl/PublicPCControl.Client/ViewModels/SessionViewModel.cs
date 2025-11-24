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

        public ICommand EndCommand { get; }
        public ICommand LaunchProgramCommand { get; }

        public SessionViewModel(SessionService sessionService, Action<string> endSession, LoggingService loggingService)
        {
            _sessionService = sessionService;
            _endSession = endSession;
            _loggingService = loggingService;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += TimerTick;
            EndCommand = new RelayCommand(_ => _endSession("manual"));
            LaunchProgramCommand = new RelayCommand(p => LaunchProgram(p as AllowedProgram), p => p is AllowedProgram);
        }

        public void BindSession(Session session, AppConfig config)
        {
            CurrentSession = session;
            Remaining = TimeSpan.FromMinutes(session.RequestedMinutes);
            AllowedPrograms.Clear();
            foreach (var program in config.AllowedPrograms)
            {
                AllowedPrograms.Add(program);
            }
            _timer.Start();
        }

        public void StopTimer() => _timer.Stop();

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