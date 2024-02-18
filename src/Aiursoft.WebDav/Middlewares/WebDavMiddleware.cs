using System.Net;
using Aiursoft.WebDav.Filesystems;
using Aiursoft.WebDav.Helpers;
using Aiursoft.WebDav.Middlewares.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aiursoft.WebDav.Middlewares
{
    /// <summary>
    /// WebDavMiddleware
    /// </summary>
    class WebDavMiddleware
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly RequestDelegate _next;

        public WebDavMiddleware(RequestDelegate next,
            IOptions<WebDavOptions> options,
            IWebDavFilesystem filesystem,
            ILogger<WebDavMiddleware> logger)
        {
            Options = options.Value;
            _next = next;
            Filesystem = filesystem;
            Logger = logger;
        }

        /// <summary>
        /// Options
        /// </summary>
        public WebDavOptions Options { get; }

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger<WebDavMiddleware> Logger { get; }

        /// <summary>
        /// Filesystem
        /// </summary>
        private IWebDavFilesystem Filesystem { get; }

        public async Task InvokeAsync(HttpContext context)
        {
            var webDavContext = new WebDavContext(
                baseUrl : $"{context.Request.Scheme}://{context.Request.Host}",
                path: (string?)context.GetRouteValue("filePath") ?? string.Empty,
                depth: context.Request.Headers["Depth"].FirstOrDefault() switch
                {
                    "0" => DepthMode.Zero,
                    "1" => DepthMode.One,
                    "infinity" => DepthMode.Infinity,
                    _ => DepthMode.None
                });

            context.SetWebDavContext(webDavContext);

            Logger.LogTrace($"HTTP Request: {context.Request.Method} {context.Request.Path}");

            var action =  context.Request.Method switch
            {
                "OPTIONS" => ProcessOptionsAsync(context),
                "PROPFIND" => ProcessPropfindAsync(context),
                "PROPPATCH" => ProcessPropPatchAsync(context),
                "MKCOL" => ProcessMKCOLAsync(context),
                "GET" => ProcessGetAsync(context),
                "PUT" => ProcessPutAsync(context),
                "HEAD" => ProcessHeadAsync(),
                "LOCK" => ProcessLockAsync(context),
                "UNLOCK" => ProcessUnlockAsync(),
                "MOVE" => ProcessMoveAsync(context),
                "DELETE" => ProcessDeleteAsync(context),
                _ => ProcessUnknown(context)
            };

            await action;
        }

        private Task ProcessOptionsAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                context.Response.Headers.Add("Allow", "OPTIONS, TRACE, GET, HEAD, PROPFIND");
            }
            else
            {
                context.Response.Headers.Add("Allow", "OPTIONS, TRACE, GET, HEAD, DELETE, PUT, POST, COPY, MOVE, MKCOL, PROPFIND, PROPPATCH, LOCK, UNLOCK");
            }
            
            context.Response.Headers.Add("DAV", "1, 2");
            context.Response.Headers.Add("MS-Author-Via", "DAV");
            return Task.CompletedTask;
        }

        private async Task ProcessGetAsync(HttpContext context)
        {
            using (var fs = await Filesystem.OpenFileStreamAsync(context.GetWebDavContext()))
            {
                await fs.CopyToAsync(context.Response.Body);
            }
        }

        private async Task ProcessMKCOLAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to create a collection.");
            }

            await Filesystem.CreateCollectionAsync(context.GetWebDavContext());
        }

        private Task ProcessPutAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to modify the file.");
            }

            return Filesystem.WriteFileAsync(context.Request.Body, context.GetWebDavContext());
        }

        private Task ProcessHeadAsync()
        {
            return Task.CompletedTask;
        }

        private async Task ProcessPropPatchAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to modify the file.");
            }
            await context.Request.ReadContentAsString();
        }

        private async Task ProcessPropfindAsync(HttpContext context)
        {
            var result = await Filesystem.FindPropertiesAsync(context.GetWebDavContext());

            context.Response.StatusCode = result.StatusCode;

            if (result is IWebDavXmlResult xmlResult)
            {
                var xml = xmlResult.ToXml(context.GetWebDavContext());

                context.Response.ContentType = "application/xml";

                await context.Response.WriteAsync(xml.ToString());
            }
        }

        private async Task ProcessLockAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to modify the file.");
            }

            await context.Request.ReadContentAsString();
            var t = new WebDavLockResult().ToXml(context.GetWebDavContext()).ToString();
            await context.Response.WriteAsync(t);
        }

        private Task ProcessUnlockAsync()
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to modify the file.");
            }

            return Task.CompletedTask;
        }

        private Task ProcessDeleteAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to delete the file.");
            }

            return Filesystem.DeleteAsync(context.GetWebDavContext());
        }

        private async Task ProcessMoveAsync(HttpContext context)
        {
            if (Options.IsReadOnly)
            {
                throw new InvalidOperationException("The server is read-only. But the request is trying to move the file.");
            }

            if (context.Request.Headers.TryGetValue("destination", out var destinations) == false
                || destinations.Any() == false)
            {
                throw new InvalidOperationException("The destination header is missing.");
            }

            var newUri = new Uri(destinations.First());

            await Filesystem.MoveToAsync(context.GetWebDavContext(), newUri.PathAndQuery.UrlDecode());

            context.Response.StatusCode = StatusCodes.Status201Created;
        }

        private Task ProcessUnknown(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return Task.CompletedTask;
        }

    }
}
