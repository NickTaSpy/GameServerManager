using System.Text.Json;

namespace GameServerManager.Shared
{
    public class SystemStats
    {
        public double TotalGiB { get; set; }
        public double UsedGiB { get; set; }
        public double AvailableGiB { get; set; }

        public float CpuUsage { get; set; }

        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
