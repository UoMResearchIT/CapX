namespace PPMTool.Data.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// Gets the path to a file in a private, consistent, maintained area of the filesystem
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetLocalApplicationFilePath(string filename)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, filename);
        }

        /// <summary>
        /// Cleans the monitored area of the file system so it doesn't explode the disk usage
        /// </summary>
        public static void CleanLocalApplicationFilePath(ILogger logger)
        {
            var localFilePath = GetLocalApplicationFilePath("");
            var files = Directory.GetFiles(localFilePath);

            logger.LogInformation($"Found {files.Count()} files in the local application filepath. Cleaning those over 7 days old if necessary...");

            foreach (var file in files)
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-7))
                {
                    logger.LogInformation($"Deleting {file}...");
                    File.Delete(file);
                }
            }
        }
    }
}
