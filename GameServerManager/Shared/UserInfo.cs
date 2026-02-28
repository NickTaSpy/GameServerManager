using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerManager.Shared
{
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public DateTime? LastAccessed { get; set; }
    }
}
