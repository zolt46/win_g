// File: PublicPCControl.Client/ViewModels/SessionViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
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
        private bool _hasAllowedPrograms;

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();

        public bool HasAllowedPrograms
        {
            get => _hasAllowedPrograms;
            private set => SetProperty(ref _hasAllowedPrograms, value);
        }

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
            AllowedPrograms.CollectionChanged += (_, _) => UpdateHasAllowedPrograms();
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
                program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
                AllowedPrograms.Add(program);
            }
            UpdateHasAllowedPrograms();
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
            UpdateHasAllowedPrograms();
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

        private void UpdateHasAllowedPrograms()
        {
            HasAllowedPrograms = AllowedPrograms.Count > 0;
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
            var mainWindow = Application.Current?.MainWindow;
            ReleaseTopmost(mainWindow);
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = program.ExecutablePath,
                    Arguments = program.Arguments,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(program.ExecutablePath) ?? string.Empty,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Verb = "open"
                });

                if (process == null)
                {
                    RestoreTopmost(mainWindow);
                    return;
                }

                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => RestoreTopmost(mainWindow);

                EnsureForegroundWindow(process, program.ExecutablePath);
                _loggingService.LogProcessStart(CurrentSession.Id, program.DisplayName, program.ExecutablePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                RestoreTopmost(mainWindow);
            }
        }

        private static void ReleaseTopmost(Window? window)
        {
            if (window == null)
            {
                return;
            }

            window.Dispatcher.Invoke(() => window.Topmost = false);
        }

        private static void RestoreTopmost(Window? window)
        {
            if (window == null)
            {
                return;
            }

            window.Dispatcher.Invoke(() =>
            {
                window.Topmost = true;
                window.Activate();
                window.Focus();
            });
        }

        private static void EnsureForegroundWindow(Process? process, string executablePath)
        {
            var stopAt = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (DateTime.UtcNow < stopAt)
            {
                var handle = TryGetWindowHandle(process, executablePath);
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SwRestore);
                    SetForegroundWindow(handle);
                    return;
                }

                System.Threading.Thread.Sleep(200);
            }
        }

        private static IntPtr TryGetWindowHandle(Process? startedProcess, string executablePath)
        {
            try
            {
                if (startedProcess != null)
                {
                    startedProcess.WaitForInputIdle(1000);
                    startedProcess.Refresh();
                    if (startedProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        return startedProcess.MainWindowHandle;
                    }
                }

                var exeName = Path.GetFileNameWithoutExtension(executablePath);
                foreach (var process in Process.GetProcessesByName(exeName))
                {
                    try
                    {
                        var mainModulePath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(mainModulePath)
                            && mainModulePath.Equals(executablePath, StringComparison.OrdinalIgnoreCase)
                            && process.MainWindowHandle != IntPtr.Zero)
                        {
                            return process.MainWindowHandle;
                        }
                    }
                    catch
                    {
                        // ignore processes that cannot be inspected
                    }
                }
            }
            catch
            {
                // ignored
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwRestore = 9;
    }
}