namespace GameServerManager.Shared
{
    public class ServerDetails
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public bool ProcessFound { get; set; }
        public bool Responding { get; set; }
    }
}
