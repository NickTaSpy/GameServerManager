using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerManager.Server.Database
{
    [Table("users")]
    public class Users
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("username")]
        [Required]
        public string Username { get; set; } = null!;

        [Column("password")]
        [Required]
        public string Password { get; set; } = null!;

        [Column("refreshtoken")]
        public string? RefreshToken { get; set; }

        [Column("tokencreated")]
        public DateTime? TokenCreated { get; set; }

        [Column("tokenexpires")]
        public DateTime? TokenExpires { get; set; }

        [Column("lastaccessed")]
        public DateTime? LastAccessed { get; set; }
    }
}