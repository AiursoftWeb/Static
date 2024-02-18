using Aiursoft.WebDav.Middlewares.Results;

namespace Aiursoft.WebDav.Locking
{
    public interface IWebDavLockProvider
    {
        Task<WebDavLockResult> LockAsync(WebDavLockScope scope, string path);

        Task UnlockAsync(string lockToken);
    }
}
