using GameServerManager.Server.Database;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.IO.Compression;
using System.IO;
using System.Text;
using GameServerManager.Server.Helpers;

namespace GameServerManager.Server.Jobs
{
    public class BackupJob : IJob
    {
        public static readonly JobKey Key = new("backup", "server");
        public const string ServerIdKey = "serverid";

        private readonly DatabaseContext _dbContext;

        public BackupJob(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var serverId = context.MergedJobDataMap.GetGuidValue(ServerIdKey);
                var server = await _dbContext.Server.FirstOrDefaultAsync(x => x.Id == serverId);

                if (server == null)
                {
                    throw new JobExecutionException($"Server ID {serverId} not found.");
                }

                var backupsPath = Path.Combine(server.Path, "Backups");
                var directory = Directory.CreateDirectory(backupsPath);

                var backupName = DateTime.UtcNow.ToString("yyyy-MM-dd") + "_Backup";

                var existingBackups = directory.GetFiles(backupName + "*", SearchOption.TopDirectoryOnly);
                backupName += existingBackups.Length > 0 ? existingBackups.Length : "";

                var finalBackupFilename = Path.ChangeExtension(Path.Combine(backupsPath, backupName), ".zip");
                ZipFileHelper.CreateFromDirectory(server.Path, finalBackupFilename, CompressionLevel.Optimal, true, fileName => !fileName.Contains("Backups"));
            }
            catch (Exception ex)
            {
                throw new JobExecutionException("", ex, false);
            }
        }
    }
}
