// File: PublicPCControl.Client/Models/WindowLog.cs
using System;

namespace PublicPCControl.Client.Models
{
    public class WindowLog
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}