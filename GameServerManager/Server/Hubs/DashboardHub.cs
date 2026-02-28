using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameServerManager.Server.Hubs
{
    [Authorize]
    public class DashboardHub : Hub
    {
    }
}
