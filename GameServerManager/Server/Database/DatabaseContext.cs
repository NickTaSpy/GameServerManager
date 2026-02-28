using Microsoft.EntityFrameworkCore;

namespace GameServerManager.Server.Database
{
    public class DatabaseContext : DbContext
    {
        public virtual DbSet<Users> Users { get; set; } = null!;
        public virtual DbSet<Server> Server { get; set; } = null!;

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
    }
}
