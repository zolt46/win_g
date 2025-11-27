// File: PublicPCControl.Client/Models/AllowedProgram.cs

namespace PublicPCControl.Client.Models
{
    public class AllowedProgram
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.ImageSource? Icon { get; set; }
    }
}