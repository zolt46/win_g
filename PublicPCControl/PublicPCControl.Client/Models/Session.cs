// File: PublicPCControl.Client/Models/Session.cs
using System;

namespace PublicPCControl.Client.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string PcName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int RequestedMinutes { get; set; }
        public int MaxExtensions { get; set; }
        public int ExtensionsUsed { get; set; }
        public int ExtensionMinutes { get; set; }
        public string EndReason { get; set; } = string.Empty;

        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    }
}