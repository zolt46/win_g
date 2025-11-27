// File: PublicPCControl.Client/Models/AppConfig.cs
using System.Collections.Generic;

namespace PublicPCControl.Client.Models
{
    public class AppConfig
    {
        public bool EnforcementEnabled { get; set; } = true;
        public bool IsAdminOnlyPc { get; set; } = false;
        public int DefaultSessionMinutes { get; set; } = 60;
        public int SessionExtensionMinutes { get; set; } = 30;
        public int MaxExtensionCount { get; set; } = 2;
        public bool AllowExtensions { get; set; } = true;
        public bool KillDisallowedProcess { get; set; } = true;
        public List<AllowedProgram> AllowedPrograms { get; set; } = new();
        public string AdminPasswordHash { get; set; } = string.Empty;
    }
}