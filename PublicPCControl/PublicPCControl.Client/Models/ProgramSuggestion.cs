// File: PublicPCControl.Client/Models/ProgramSuggestion.cs
using System.Windows.Media;

namespace PublicPCControl.Client.Models
{
    public class ProgramSuggestion
    {
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
    }
}
