using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebDav.Builders
{
    class WebDavBuilder : IWebDavBuilder
    {
        public WebDavBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
