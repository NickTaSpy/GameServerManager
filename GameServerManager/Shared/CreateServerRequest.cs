using System.ComponentModel.DataAnnotations;

namespace GameServerManager.Shared
{
    public class CreateServerRequest
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Path { get; set; } = null!;

        [Required]
        public string Filename { get; set; } = null!;

        public string? Arguments { get; set; }

        [Required]
        public int Port { get; set; }
    }
}
