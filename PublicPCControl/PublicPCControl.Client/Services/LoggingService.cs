// File: PublicPCControl.Client/Services/LoggingService.cs
using System;
using PublicPCControl.Client.Data;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public class LoggingService
    {
        private readonly LogRepository _logRepository;
        private readonly AuditLogWriter _auditLogWriter;

        public LoggingService(LogRepository logRepository, AuditLogWriter auditLogWriter)
        {
            _logRepository = logRepository;
            _auditLogWriter = auditLogWriter;
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

            _auditLogWriter.WriteLine("PROCESS_START", $"session={sessionId}, name={name}, path={path}");
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

            _auditLogWriter.WriteLine("PROCESS_END", $"session={sessionId}, name={name}, path={path}, reason={reason}");
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

            _auditLogWriter.WriteLine("WINDOW", $"session={sessionId}, process={process}, title={title}");
        }
    }
}
