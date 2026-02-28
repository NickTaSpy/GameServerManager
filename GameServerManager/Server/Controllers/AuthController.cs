using GameServerManager.Server.Database;
using GameServerManager.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GameServerManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _dbContext;

        public AuthController(IConfiguration configuration, DatabaseContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request, CancellationToken ct)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username, ct);

            if (user == null || !Bcrypt.Verify(request.Password, user.Password))
            {
                return BadRequest("Incorrect username or password.");
            }

            var token = CreateToken(user);
            SetRefreshToken(user);
            await _dbContext.SaveChangesAsync(ct);

            return Ok(token);
        }

        //[HttpPost("register")]
        //public async Task<ActionResult> Register(UserLoginDto request, CancellationToken ct)
        //{
        //    if (await _dbContext.Users.AnyAsync(x => x.Username == request.Username, ct))
        //    {
        //        return BadRequest("This username is taken. Choose a different one.");
        //    }

        //    var hashedPassword = Bcrypt.HashPassword(request.Password);

        //    await _dbContext.Users.AddAsync(new Users
        //    {
        //        Username = request.Username,
        //        Password = hashedPassword
        //    }, ct);

        //    await _dbContext.SaveChangesAsync(ct);

        //    return Ok();
        //}

        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken(CancellationToken ct)
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken is null)
            {
                BadRequest("No refresh token was provided.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, ct);

            if (user is null)
            {
                return Unauthorized("Invalid Refresh Token.");
            }

            if (user.TokenExpires < DateTime.UtcNow)
            {
                return Unauthorized("Token expired.");
            }

            var token = CreateToken(user);
            SetRefreshToken(user);
            await _dbContext.SaveChangesAsync(ct);

            return Ok(token);
        }

        [Authorize]
        [HttpPost("createShortTermToken")]
        public async Task<ActionResult<string>> CreateShortTermToken(CancellationToken ct)
        {
            var nameClaim = User.FindFirst(ClaimTypes.Name);

            if (nameClaim is null)
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == nameClaim.Value, ct);

            if (user is null)
            {
                return Unauthorized();
            }

            return CreateToken(user, TimeSpan.FromMinutes(1));
        }

        private string CreateToken(Users user, TimeSpan tokenDuration = default)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow + (tokenDuration == default ? _configuration.GetValue<TimeSpan>("AppSettings:TokenDuration") : tokenDuration),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetRefreshToken(Users user)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var created = DateTime.UtcNow;
            var expires = created.AddDays(7);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expires
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);

            user.RefreshToken = token;
            user.TokenCreated = created;
            user.TokenExpires = expires;
        }
    }
}
