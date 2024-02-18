using Aiursoft.WebDav.Builders;
using Aiursoft.WebDav.Filesystems;
using Aiursoft.WebDav.Filesystems.LocalFilesystems;
using Aiursoft.WebDav.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebDav
{
    public static class Extensions
    {
        public static IWebDavBuilder AddWebDav(this IServiceCollection services, Action<WebDavOptions>? options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }

            return new WebDavBuilder(services);
        }

        public static IWebDavBuilder AddFilesystem(this IWebDavBuilder builder, Action<LocalFilesystemOptions>? options = null)
        {
            if (options != null)
            {
                builder.Services.Configure(options);
            }

            builder.Services.AddTransient<IWebDavFilesystem, LocalFilesystem>();

            return builder;
        }

        public static IApplicationBuilder UseWebDav(this IApplicationBuilder builder, PathString basePath)
        {
            builder.Map(basePath, b =>
            {
                b.UseRouting();
                b.UseEndpoints(endpoints => endpoints.MapWebDav());
            });

            return builder;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static IEndpointConventionBuilder MapWebDav(this IEndpointRouteBuilder endpoints)
        {
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<WebDavMiddleware>()
                .Build();

            return endpoints.Map("{*filePath}", pipeline).WithDisplayName("WebDavSharp");
        }
    }
}
