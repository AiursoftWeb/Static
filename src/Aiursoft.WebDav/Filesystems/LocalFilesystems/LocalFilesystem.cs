using System.Net;
using Aiursoft.WebDav.Middlewares;
using Aiursoft.WebDav.Middlewares.Results;
using Microsoft.Extensions.Options;

namespace Aiursoft.WebDav.Filesystems.LocalFilesystems
{
    /// <summary>
    /// LocalFilesystem
    /// </summary>
    public class LocalFilesystem : IWebDavFilesystem
    {
        private readonly LocalFilesystemOptions _options;

        public LocalFilesystem(IOptions<LocalFilesystemOptions> options)
        {
            _options = options.Value;
        }

        private string GetLocalPath(string path)
        {
            if(path.StartsWith("/"))
            {
                path = path[1..];
            }

            var fullPath = Path.Combine(_options.SourcePath, path);

            if (fullPath.StartsWith(_options.SourcePath) == false)
            {
                throw new InvalidOperationException("Invalid path for local filesystem! Path: " + path);
            }

            return fullPath;
        }

        public Task<Stream> OpenFileStreamAsync(WebDavContext context)
        {
            var file = new FileInfo(GetLocalPath(context.Path));
            return Task.FromResult<Stream>(file.OpenRead());
        }

        public async Task WriteFileAsync(Stream stream, WebDavContext context)
        {
            var file = new FileInfo(GetLocalPath(context.Path));

            await using Stream fs = file.OpenWrite();
            fs.SetLength(0);
            await stream.CopyToAsync(fs);
        }

        public Task<IWebDavResult> FindPropertiesAsync(WebDavContext context)
        {
            var fullPath = GetLocalPath(context.Path);

            //is directory
            if (Directory.Exists(fullPath))
            {
                return Task.FromResult<IWebDavResult>(new WebDavCollectionsResult(new DirectoryInfo(fullPath)));
            }
            //is file
            else if (File.Exists(fullPath))
            {
                return Task.FromResult<IWebDavResult>(new WebDavFile(new FileInfo(fullPath)));
            }
            //not found
            else
            {
                return Task.FromResult<IWebDavResult>(new WebDavFileNotFoundResult());
            }
        }

        public Task<IWebDavResult> PatchPropertiesAsync(WebDavContext context)
        {
            return Task.FromResult<IWebDavResult>(new WebDavNoContentResult(HttpStatusCode.OK));
        }

        public Task<bool> DeleteAsync(WebDavContext context)
        {
            var fullpath = GetLocalPath(context.Path);

            if (Directory.Exists(fullpath))
            {
                Directory.Delete(fullpath, true);
            }
            else if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
            }
            else
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<IWebDavResult> CreateCollectionAsync(WebDavContext context)
        {
            var fullPath = GetLocalPath(context.Path);
            Directory.CreateDirectory(fullPath);
            return Task.FromResult<IWebDavResult>(new WebDavNoContentResult(HttpStatusCode.Created));
        }

        public Task<bool> MoveToAsync(WebDavContext context, string path)
        {
            var localPathFrom = GetLocalPath(context.Path);
            var localPathTo = GetLocalPath(path);
            
            Directory.Move(localPathFrom, localPathTo);

            return Task.FromResult(true);
        }
    }
}
