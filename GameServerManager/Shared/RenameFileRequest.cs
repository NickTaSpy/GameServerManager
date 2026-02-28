using System.ComponentModel.DataAnnotations;

namespace GameServerManager.Shared
{
    public class RenameFileRequest
    {
        public Guid ServerId { get; set; }

        [Required]
        public string Path { get; set; } = null!;

        [Required]
        public string NewName { get; set; } = null!;
    }
}
