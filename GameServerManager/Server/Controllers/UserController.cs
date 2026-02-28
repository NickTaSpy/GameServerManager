using GameServerManager.Server.Database;
using GameServerManager.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public UserController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<UserInfo>>> List(CancellationToken ct)
        {
            return await _dbContext.Users.Select(x => new UserInfo { Id = x.Id, Username = x.Username, LastAccessed = x.LastAccessed }).ToListAsync(ct);
        }
    }
}
