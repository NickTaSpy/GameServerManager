using GameServerManager.Shared;
using System.IO;
using System.IO.Compression;

namespace GameServerManager.Server.Helpers
{
    public static class FileHelper
    {
        private static readonly EnumerationOptions DefaultEnumerationOptions = new();
        private static readonly EnumerationOptions DefaultEnumerationOptionsRecursive = new()
        {
            RecurseSubdirectories = true
        };

        public static async Task<List<FileDetails>> GetAllDirectoryFileDetails(string path, CancellationToken ct = default)
        {
            var di = new DirectoryInfo(path);

            var directoriesTasks = di.GetDirectories("*", DefaultEnumerationOptions).Select(x => CreateFileDetails(x, ct));
            var files = di.GetFiles("*", DefaultEnumerationOptions).Select(x => CreateFileDetails(x));

            return (await Task.WhenAll(directoriesTasks)).Union(files).ToList();
        }

        public static void RenameFileOrDirectory(string path, string newName)
        {
            path = Path.TrimEndingDirectorySeparator(path);

            var directoryPath = Path.GetDirectoryName(path);

            if (directoryPath is null)
            {
                throw new DirectoryNotFoundException();
            }

            Directory.Move(path, Path.Combine(directoryPath, newName));
        }

        public static void DeleteFileOrDirectory(string path)
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                Directory.Delete(path, true);
            }
            else
            {
                File.Delete(path);
            }
        }

        public static FileStream GetFileOrZippedDirectory(string path, bool isDirectory)
        {
            if (!isDirectory)
            {
                return File.OpenRead(path);
            }

            string? tempFile = null;

            try
            {
                tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.CreateFromDirectory(path, tempFile, CompressionLevel.Optimal, true);

                return new FileStream(tempFile, new FileStreamOptions
                {
                    Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous
                });
            }
            catch
            {
                if (tempFile is not null)
                {
                    File.Delete(tempFile);
                }

                throw;
            }
        }

        public static bool IsDirectory(string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

        private static FileDetails CreateFileDetails(FileInfo fileInfo) => new()
        {
            Name = fileInfo.Name,
            SizeBytes = fileInfo.Length,
            ModifiedAt = fileInfo.LastWriteTimeUtc,
            CreatedAt = fileInfo.CreationTimeUtc
        };

        private static async Task<FileDetails> CreateFileDetails(DirectoryInfo directoryInfo, CancellationToken ct = default)
        {
            long dirSize;

            try
            {
                dirSize = await Task.Run(() => directoryInfo.EnumerateFiles("*", DefaultEnumerationOptionsRecursive).Sum(x => x.Length), ct).WaitAsync(ct);
            }
            catch (Exception ex) when (ex is TaskCanceledException or TimeoutException)
            {
                dirSize = 0;
            }

            return new FileDetails
            {
                Name = directoryInfo.Name,
                SizeBytes = dirSize,
                ModifiedAt = directoryInfo.LastWriteTimeUtc,
                CreatedAt = directoryInfo.CreationTimeUtc,
                IsDirectory = true
            };
        }
    }
}
