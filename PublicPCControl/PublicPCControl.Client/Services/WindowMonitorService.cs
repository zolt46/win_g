// File: PublicPCControl.Client/Services/WindowMonitorService.cs
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace PublicPCControl.Client.Services
{
    public class WindowMonitorService : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly Action<string, string> _onWindowChanged;
        private string _lastTitle = string.Empty;

        public WindowMonitorService(Action<string, string> onWindowChanged)
        {
            _onWindowChanged = onWindowChanged;
            _timer = new System.Timers.Timer(3000);
            _timer.Elapsed += TimerElapsed;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            var handle = GetForegroundWindow();
            var sb = new StringBuilder(256);
            GetWindowText(handle, sb, sb.Capacity);
            var title = sb.ToString();
            GetWindowThreadProcessId(handle, out var pid);
            var process = Process.GetProcessById((int)pid);
            if (!string.Equals(_lastTitle, title, StringComparison.Ordinal))
            {
                _lastTitle = title;
                _onWindowChanged(process.ProcessName, title);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}