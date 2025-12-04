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

        public void WriteLine(int sessionId, string category, string message)
        {
            var path = Path.Combine(_logDirectory, $"audit-{DateTime.Now:yyyyMMdd}.csv");
            var line = string.Join(',',
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Escape(category),
                sessionId,
                Escape(message));
            lock (_sync)
            {
                EnsureHeader(path);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }

        private static void EnsureHeader(string path)
        {
            if (!File.Exists(path))
            {
                File.AppendAllText(path, "timestamp,category,sessionId,message" + Environment.NewLine);
            }
        }

        private static string Escape(string value)
        {
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
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
