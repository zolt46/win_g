// File: PublicPCControl.Client/Models/ProcessLog.cs
using System;

namespace PublicPCControl.Client.Models
{
    public class ProcessLog
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string EndReason { get; set; } = string.Empty;
    }
}