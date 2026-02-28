using System.ComponentModel.DataAnnotations;

namespace GameServerManager.Shared
{
    public class ExecuteCommandRequest
    {
        public Guid ServerId { get; set; }

        [Required]
        public string Command { get; set; } = null!;
    }
}
