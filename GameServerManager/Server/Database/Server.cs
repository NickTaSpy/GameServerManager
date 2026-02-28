using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerManager.Server.Database
{
    [Table("server")]
    public class Server
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        [Required]
        public string Name { get; set; } = null!;

        [Column("path")]
        [Required]
        public string Path { get; set; } = null!;

        [Column("filename")]
        [Required]
        public string Filename { get; set; } = null!;

        [Column("arguments")]
        public string? Arguments { get; set; }

        [Column("port")]
        [Required]
        public int Port { get; set; }
    }
}
