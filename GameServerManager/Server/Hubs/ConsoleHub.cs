using GameServerManager.Server.Database;
using GameServerManager.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameServerManager.Server.Hubs
{
    [Authorize]
    public class ConsoleHub : Hub
    {
        private readonly DatabaseContext _dbContext;
        private readonly ConsoleReaderService _consoleReader;

        public ConsoleHub(DatabaseContext dbContext, ConsoleReaderService consoleReader)
        {
            _dbContext = dbContext;
            _consoleReader = consoleReader;
        }

        public async Task SubscribeServer(Guid serverId)
        {
            var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId);

            if (server is null)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, serverId.ToString());

            var logsDirectory = Path.Combine(server.Path, "logs");
            var logFile = Path.Combine(logsDirectory, "latest.log");

            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveLogs", await File.ReadAllBytesAsync(logFile));

            _consoleReader.StartWatch(logsDirectory, logFile, serverId);
        }
    }
}
