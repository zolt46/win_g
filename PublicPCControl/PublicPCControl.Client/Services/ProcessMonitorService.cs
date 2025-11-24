// File: PublicPCControl.Client/Services/ProcessMonitorService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public class ProcessMonitorService : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly LoggingService _loggingService;
        private readonly Func<AppConfig> _getConfig;
        private readonly Func<Session?> _getSession;

        public ProcessMonitorService(LoggingService loggingService, Func<AppConfig> getConfig, Func<Session?> getSession)
        {
            _loggingService = loggingService;
            _getConfig = getConfig;
            _getSession = getSession;
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += TimerElapsed;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var config = _getConfig();
                var session = _getSession();
                if (session == null || !config.KillDisallowedProcess) return;
                var allowedPaths = new HashSet<string>(config.AllowedPrograms.Select(a => a.ExecutablePath), StringComparer.OrdinalIgnoreCase);
                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    try
                    {
                        var path = p.MainModule?.FileName ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(path)) continue;
                        if (allowedPaths.Contains(path) || path.Contains("\\Windows\\"))
                            continue;

                        _loggingService.LogProcessEnd(session.Id, p.ProcessName, path, "blocked");
                        p.Kill();
                    }
                    catch (Exception)
                    {
                        // ignore access denied
                    }
                }
            }
            catch (Exception)
            {
                // last line of defense to prevent timer thread crash
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}