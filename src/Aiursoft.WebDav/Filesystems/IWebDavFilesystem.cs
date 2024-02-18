using Aiursoft.WebDav.Middlewares;

namespace Aiursoft.WebDav.Filesystems
{
    public interface IWebDavFilesystem
    {
        Task<IWebDavResult> FindPropertiesAsync(WebDavContext context);

        Task<IWebDavResult> PatchPropertiesAsync(WebDavContext context);

        Task<IWebDavResult> CreateCollectionAsync(WebDavContext context);

        Task<Stream> OpenFileStreamAsync(WebDavContext context);

        Task WriteFileAsync(Stream stream, WebDavContext context);

        Task<bool> DeleteAsync(WebDavContext context);

        Task<bool> MoveToAsync(WebDavContext context, string path);

        
    }
}
