using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebDav.Builders
{
    public interface IWebDavBuilder
    {
        IServiceCollection Services { get; }
    }
}
