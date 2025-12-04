// File: PublicPCControl.Client/Services/ErrorReporter.cs
using System;
using System.IO;
using System.Text;

namespace PublicPCControl.Client.Services
{
    public static class ErrorReporter
    {
        private static readonly object _sync = new object();
        private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

        public static void Log(string source, Exception exception)
        {
            try
            {
                var content = new StringBuilder()
                    .AppendLine("====")
                    .AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    .AppendLine($"Source: {source}")
                    .AppendLine(exception.ToString())
                    .ToString();

                lock (_sync)
                {
                    File.AppendAllText(_logPath, content);
                }
            }
            catch
            {
                // 로깅 실패 시에는 더 이상 예외를 전파하지 않는다.
            }
        }
    }
}
