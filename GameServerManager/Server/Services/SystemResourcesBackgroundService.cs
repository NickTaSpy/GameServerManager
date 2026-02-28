using GameServerManager.Server.Helpers;
using GameServerManager.Server.Hubs;
using GameServerManager.Shared;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace GameServerManager.Server.Services
{
    public sealed class SystemResourcesBackgroundService : BackgroundService
    {
        private static readonly double KBToGiB = Math.Pow(2, 30) / Math.Pow(10, 3);
        private static readonly double KiBToGiB = Math.Pow(1024, 2);
        private static readonly double BToGiB = Math.Pow(1024, 3);

        private readonly SystemStats _stats = new();

        private readonly PerformanceCounter? _memoryCounter;
        private readonly PerformanceCounter? _cpuCounter;

        private readonly IHubContext<DashboardHub> _hub;
        private readonly ILogger<SystemResourcesBackgroundService> _logger;

        public SystemResourcesBackgroundService(IHubContext<DashboardHub> hubContext, ILogger<SystemResourcesBackgroundService> logger)
        {
            _hub = hubContext;
            _logger = logger;

            if (OS.IsWindows)
            {
                _memoryCounter = new PerformanceCounter("Memory", "Available KBytes");
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _stats.TotalGiB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / BToGiB;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await UpdateSystemStats();
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogEvents.SystemResources, ex, "Exception while updating system resources.");
                    continue;
                }

                await _hub.Clients.All.SendAsync("ReceiveStats", _stats, stoppingToken);
            }
        }

        private Task UpdateSystemStats()
        {
            _stats.Timestamp = DateTime.UtcNow;

            if (OS.IsWindows)
            {
                UpdateMemoryStats_Windows();
                UpdateCPUStats_Windows();
            }
            else if (OS.IsLinux)
            {
                return Task.WhenAll(UpdateMemoryStatsAsync_Linux(), UpdateCPUStatsAsync_Linux());
            }

            return Task.CompletedTask;
        }

        [SupportedOSPlatform("Windows")]
        private void UpdateMemoryStats_Windows()
        {
            if (_memoryCounter is null)
            {
                return;
            }

            _stats.AvailableGiB = _memoryCounter.NextValue() / KBToGiB;
            _stats.UsedGiB = _stats.TotalGiB - _stats.AvailableGiB;
        }

        [SupportedOSPlatform("Windows")]
        private void UpdateCPUStats_Windows()
        {
            if (_cpuCounter is null)
            {
                return;
            }

            _stats.CpuUsage = _cpuCounter.NextValue();
        }

        [SupportedOSPlatform("Linux")]
        private async Task UpdateMemoryStatsAsync_Linux()
        {
            var psi = new ProcessStartInfo("free", "-k")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);

            if (proc is null)
            {
                _logger.LogError(LogEvents.SystemResources, "Failed to start memory usage process");
                return;
            }

            var output = await proc.StandardOutput.ReadToEndAsync();
            var lines = output.Split('\n');
            var memCols = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            _stats.TotalGiB = double.Parse(memCols[1]) / KiBToGiB;
            _stats.UsedGiB = double.Parse(memCols[2]) / KiBToGiB;
            _stats.AvailableGiB = double.Parse(memCols[6]) / KiBToGiB;
        }

        [SupportedOSPlatform("Linux")]
        private async Task UpdateCPUStatsAsync_Linux()
        {
            var psi = new ProcessStartInfo("top", "bn2")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);

            if (proc is null)
            {
                _logger.LogError(LogEvents.SystemResources, "Failed to start CPU usage process");
                return;
            }

            var output = await proc.StandardOutput.ReadToEndAsync();
            var lines = output.Split('\n');
            var cpuLine = Array.FindLast(lines, x => x.StartsWith("%Cpu(s)"));

            if (cpuLine is null)
            {
                _logger.LogError(LogEvents.SystemResources, "Failed to find CPU usage from output.");
                return;
            }

            var values = cpuLine.Split(',', ':');

            if (values.Length < 4 || !float.TryParse(values[4][..^3], out var idleTime))
            {
                _logger.LogError(LogEvents.SystemResources, "Failed to parse CPU usage");
                return;
            }

            _stats.CpuUsage = 100f - (idleTime / Environment.ProcessorCount);
        }

        public override void Dispose()
        {
            _memoryCounter?.Dispose();
            _cpuCounter?.Dispose();
            base.Dispose();
        }
    }
}
