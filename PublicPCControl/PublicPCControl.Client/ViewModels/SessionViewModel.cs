// File: PublicPCControl.Client/ViewModels/SessionViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new ObservableCollection<AllowedProgram>();

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
                if (program.Icon == null)
                {
                    program.Icon = IconHelper.LoadIcon(program.ExecutablePath);
                }
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
                    BringToFront(program.ExecutablePath, null);
                    RestoreTopmost(mainWindow);
                    return;
                }

                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => RestoreTopmost(mainWindow);

                BringToFront(program.ExecutablePath, process);
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

        private static void BringToFront(string executablePath, Process? process)
        {
            try
            {
                var handle = WaitForMainWindow(process);
                if (handle == IntPtr.Zero)
                {
                    handle = FindExistingWindow(executablePath);
                }

                if (handle != IntPtr.Zero)
                {
                    ActivateHandle(handle);
                }
            }
            catch
            {
                // ignored
            }

            return IntPtr.Zero;
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr WaitForMainWindow(Process? process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (process.HasExited)
                    {
                        break;
                    }

                    process.WaitForInputIdle(1000);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                    // ignored
                }

                System.Threading.Thread.Sleep(150);
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindExistingWindow(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return IntPtr.Zero;
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.Equals(path, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }

                    var fallback = FindVisibleWindowHandle(proc.Id);
                    if (fallback != IntPtr.Zero)
                    {
                        return fallback;
                    }
                }
                catch
                {
                    // ignore processes we cannot inspect
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindVisibleWindowHandle(int processId)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != processId || !IsWindowVisible(hWnd))
                {
                    return true;
                }

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static void ActivateHandle(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetWindowPos(handle, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SwRestore = 9;
        private static readonly IntPtr HwndTopmost = new(-1);
        private static readonly IntPtr HwndNoTopmost = new(-2);
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpShowWindow = 0x0040;
    }
}