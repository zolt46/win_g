// File: PublicPCControl.Client/Services/AuditLogWriter.cs
using System;
using System.IO;

namespace PublicPCControl.Client.Services
{
    public class AuditLogWriter
    {
        private readonly string _logDirectory;
        private readonly object _sync = new();

        public AuditLogWriter(string? logDirectory = null)
        {
            _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? GetDefaultDirectory()
                : logDirectory!;
            Directory.CreateDirectory(_logDirectory);
        }

        public void WriteLine(string category, string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t[{category}] {message}";
            var path = Path.Combine(_logDirectory, $"audit-{DateTime.Now:yyyyMMdd}.log");
            lock (_sync)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }

        private static string GetDefaultDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }

            return Path.Combine(localAppData, "PublicPCControl", "Logs");
        }
    }
}
