namespace Aiursoft.WebDav.Filesystems.LocalFilesystems
{
    public static class LocalFilesystemExtensions
    {
        public static string GetLocalPath(this LocalFilesystemOptions options, string path)
        {
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            var fullpath = Path.Combine(options.SourcePath, path);

            if (fullpath.StartsWith(options.SourcePath) == false)
            {
                throw new InvalidOperationException("Invalid path for local filesystem! Path: " + path);
            }

            return fullpath;
        }
    }
}
