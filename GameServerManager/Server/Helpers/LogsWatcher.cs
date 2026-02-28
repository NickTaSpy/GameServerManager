namespace GameServerManager.Server.Helpers
{
    public sealed class LogsWatcher : IDisposable
    {
        public delegate void LogsReceivedDelegate(Guid serverId, byte[] logs);
        public event LogsReceivedDelegate? LogsReceived;

        public readonly Guid ServerId;

        private readonly FileSystemWatcher _watcher = new();
        private readonly FileInfo _fileInfo;

        private long _lastLength;

        public LogsWatcher(string directory, string logFile, Guid serverId)
        {
            _watcher.Path = directory;
            _watcher.Filter = Path.GetFileName(logFile);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

            ServerId = serverId;
            _fileInfo = new FileInfo(Path.Combine(directory, logFile));
            _lastLength = _fileInfo.Length;
        }

        public void Start()
        {
            if (_watcher.EnableRaisingEvents)
            {
                return;
            }

            _watcher.Changed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            using var reader = _fileInfo.OpenRead();

            var length = reader.Length;
            byte[]? resultData = null;

            if (length > _lastLength)
            {
                reader.Position = _lastLength;
                resultData = new byte[length - _lastLength];
                reader.Read(resultData, 0, resultData.Length);
            }
            else if (length <= _lastLength && length > 0)
            {
                resultData = new byte[length];
                reader.Read(resultData, 0, resultData.Length);
            }

            _lastLength = length;

            if (resultData?.Length > 0)
            {
                LogsReceived?.Invoke(ServerId, resultData);
            }
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Dispose();
        }
    }
}
