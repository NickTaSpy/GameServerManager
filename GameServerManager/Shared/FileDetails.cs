namespace GameServerManager.Shared
{
    public class FileDetails
    {
        public string Name { get; set; } = null!;
        public DateTime ModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? SizeBytes { get; set; }
        public bool IsDirectory { get; set; }
    }
}
