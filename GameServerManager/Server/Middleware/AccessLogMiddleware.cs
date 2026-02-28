using GameServerManager.Server.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameServerManager.Server.Middleware
{
    public class AccessLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AccessLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DatabaseContext dbContext)
        {
            var nameClaim = context.User.FindFirst(ClaimTypes.Name);

            if (nameClaim is null)
            {
                await _next(context);
                return;
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == nameClaim.Value);

            if (user is null)
            {
                return;
            }

            user.LastAccessed = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            await _next(context);
        }
    }

    public static class AccessLogMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestCulture(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AccessLogMiddleware>();
        }
    }
}
