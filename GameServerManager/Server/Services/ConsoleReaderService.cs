using GameServerManager.Server.Helpers;
using GameServerManager.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GameServerManager.Server.Services
{
    public sealed class ConsoleReaderService : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, LogsWatcher> _watchers = new();
        private readonly IHubContext<ConsoleHub> _hub;

        public ConsoleReaderService(IHubContext<ConsoleHub> hubContext)
        {
            _hub = hubContext;
        }

        public void StartWatch(string directory, string logFile, Guid serverId)
        {
            var logsWatcher = new LogsWatcher(directory, logFile, serverId);

            if (_watchers.TryAdd(serverId, logsWatcher))
            {
                logsWatcher.LogsReceived += OnLogsReceived;
                logsWatcher.Start();
            }
        }

        private void OnLogsReceived(Guid serverId, byte[] logs)
        {
            _hub.Clients.Group(serverId.ToString()).SendAsync("ReceiveLogs", logs);
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.LogsReceived -= OnLogsReceived;
                watcher.Dispose();
            }
        }
    }
}
