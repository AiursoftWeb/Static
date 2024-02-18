using System.Net;
using Aiursoft.WebDav.Middlewares;
using Aiursoft.WebDav.Middlewares.Results;
using Microsoft.Extensions.Options;

namespace Aiursoft.WebDav.Filesystems.LocalFilesystems
{
    /// <summary>
    /// LocalFilesystem
    /// </summary>
    class LocalFilesystem : IWebDavFilesystem
    {
        public LocalFilesystem(IOptions<LocalFilesystemOptions> options)
        {
            Options = options.Value;
        }

        /// <summary>
        /// Options
        /// </summary>
        public LocalFilesystemOptions Options { get; }

        private string GetLocalPath(string path)
        {
            if(path.StartsWith("/"))
            {
                path = path[1..];
            }

            var fullpath = Path.Combine(Options.SourcePath, path);

            if (fullpath.StartsWith(Options.SourcePath) == false)
            {
                throw new InvalidOperationException("Invalid path for local filesystem! Path: " + path);
            }

            return fullpath;
        }

        public Task<Stream> OpenFileStreamAsync(WebDavContext context)
        {
            var file = new FileInfo(GetLocalPath(context.Path));
            return Task.FromResult(file.OpenRead() as Stream);
        }

        public async Task WriteFileAsync(Stream stream, WebDavContext context)
        {
            var file = new FileInfo(GetLocalPath(context.Path));

            using (Stream fs = file.OpenWrite())
            {
                //clear content
                fs.SetLength(0);

                await stream.CopyToAsync(fs);
            }
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
