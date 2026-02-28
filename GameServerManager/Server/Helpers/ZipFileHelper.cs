using System.IO.Compression;
using System.Text;

namespace GameServerManager.Server.Helpers
{
    public class ZipFileHelper
    {
        public static void CreateFromDirectory(
            string sourceDirectoryName,
            string destinationArchiveFileName,
            CompressionLevel compressionLevel,
            bool includeBaseDirectory,
            Predicate<string> filter
            )
        {
            if (string.IsNullOrEmpty(sourceDirectoryName))
            {
                throw new ArgumentNullException(nameof(sourceDirectoryName));
            }

            if (string.IsNullOrEmpty(destinationArchiveFileName))
            {
                throw new ArgumentNullException(nameof(destinationArchiveFileName));
            }

            var filesToAdd = Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories);

            if (includeBaseDirectory)
            {
                sourceDirectoryName = Path.GetDirectoryName(sourceDirectoryName)
                    ?? throw new ArgumentException("Source directory has no parent directory.", nameof(sourceDirectoryName));
            }

            using var zipFileStream = new FileStream(destinationArchiveFileName, FileMode.Create);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);

            foreach (var file in filesToAdd)
            {
                if (!filter(file))
                {
                    continue;
                }

                var entryName = file.Remove(0, sourceDirectoryName.Length + 1);
                archive.CreateEntryFromFile(file, entryName, compressionLevel);
            }
        }
    }
}
