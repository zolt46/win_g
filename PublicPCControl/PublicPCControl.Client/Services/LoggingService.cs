// File: PublicPCControl.Client/Services/LoggingService.cs
using System;
using PublicPCControl.Client.Data;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public class LoggingService
    {
        private readonly LogRepository _logRepository;

        public LoggingService(LogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public void LogProcessStart(int sessionId, string name, string path)
        {
            _logRepository.InsertProcessLog(new ProcessLog
            {
                SessionId = sessionId,
                ProcessName = name,
                ExecutablePath = path,
                StartedAt = DateTime.Now,
                EndReason = "running"
            });
        }

        public void LogProcessEnd(int sessionId, string name, string path, string reason)
        {
            _logRepository.InsertProcessLog(new ProcessLog
            {
                SessionId = sessionId,
                ProcessName = name,
                ExecutablePath = path,
                StartedAt = DateTime.Now,
                EndedAt = DateTime.Now,
                EndReason = reason
            });
        }

        public void LogWindowChange(int sessionId, string process, string title)
        {
            _logRepository.InsertWindowLog(new WindowLog
            {
                SessionId = sessionId,
                ProcessName = process,
                WindowTitle = title,
                ChangedAt = DateTime.Now
            });
        }
    }
}