using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Encodings.Web;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.Static.Extensions;
using Aiursoft.Static.Middlewares;
using Aiursoft.Static.Models.Configuration;
using Aiursoft.WebDav;
using Microsoft.Extensions.FileProviders;

namespace Aiursoft.Static.Handlers;

public class StaticHandler : ExecutableCommandHandlerBuilder
{
    protected override string Name => "static";

    protected override string Description => "Start a static file server.";
    
    protected override Option[] GetCommandOptions() =>
    [
        OptionsProvider.PortOption,
        OptionsProvider.FolderOption,
        OptionsProvider.AllowDirectoryBrowsingOption,
        OptionsProvider.MirrorWebSiteOption,
        OptionsProvider.CachedMirroredFilesOption,
        OptionsProvider.EnableWebDavOption,
        OptionsProvider.WebDavCanWriteOption,
        OptionsProvider.NotFoundPageOption,
    ];

    protected override async Task Execute(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForOption(OptionsProvider.FolderOption)!;
        var port = context.ParseResult.GetValueForOption(OptionsProvider.PortOption);
        var allowDirectoryBrowsing = context.ParseResult.GetValueForOption(OptionsProvider.AllowDirectoryBrowsingOption);
        
        var autoMirror = context.ParseResult.GetValueForOption(OptionsProvider.MirrorWebSiteOption);
        var cacheMirror = context.ParseResult.GetValueForOption(OptionsProvider.CachedMirroredFilesOption);
        
        var enableWebDav = context.ParseResult.GetValueForOption(OptionsProvider.EnableWebDavOption);
        var webDavCanWrite = context.ParseResult.GetValueForOption(OptionsProvider.WebDavCanWriteOption);
        
        var notFoundPage = context.ParseResult.GetValueForOption(OptionsProvider.NotFoundPageOption);
        
        if (autoMirror is not null && allowDirectoryBrowsing)
        {
            throw new InvalidOperationException("You cannot enable directory browsing when you are mirroring a website. This is because the directory browsing will be blocked by the mirror middleware.");
        }
        if (autoMirror is null && cacheMirror)
        {
            throw new InvalidOperationException("You cannot cache mirrored files when you are not mirroring a website.");
        }
        if (!enableWebDav && webDavCanWrite)
        {
            throw new InvalidOperationException("You cannot enable WebDAV write access when WebDAV is not enabled.");
        }
        
        var app = BuildApp(path, port, allowDirectoryBrowsing, autoMirror, cacheMirror, enableWebDav, webDavCanWrite, notFoundPage);
        await app.RunAsync();
    }

    /// <summary>
    /// Builds and configures a web application.
    /// </summary>
    /// <param name="path">The physical path to the root directory of the web application.</param>
    /// <param name="port">The port number on which the web application will be hosted.</param>
    /// <param name="allowDirectoryBrowsing">Whether to allow directory browsing or not.</param>
    /// <param name="autoMirror">The URL of the website to mirror (optional).</param>
    /// <param name="cacheMirror">Whether to cache the mirrored files or not.</param>
    /// <param name="enableWebDav">Whether to enable WebDAV or not.</param>
    /// <param name="webDavCanWrite">Whether to allow write access for the WebDAV server or not.</param>
    /// <param name="notFoundPage">The path to the custom 404 page (optional).</param>
    /// <returns>A built and configured instance of WebApplication.</returns>
    private static WebApplication BuildApp(
        string path, 
        int port, 
        bool allowDirectoryBrowsing, 
        string? autoMirror, 
        bool cacheMirror,
        bool enableWebDav,
        bool webDavCanWrite,
        string? notFoundPage)
    {
        var contentRoot = Path.GetFullPath(path);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = contentRoot,
            ContentRootPath = contentRoot
        });
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "mm:ss:fff ";
            options.UseUtcTimestamp = true;
        });
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
        builder.Services.Configure<MirrorConfiguration>(options =>
        {
            options.MirrorWebSite = autoMirror;
            options.CachedMirroredFiles = cacheMirror;
        });
        builder.Services.Configure<NotFoundConfiguration>(options =>
        {
            options.NotFoundPage = notFoundPage;
        });

        if (enableWebDav)
        {
            var readonlyWebDav = !webDavCanWrite;
            builder.Services
                .AddWebDav(x => x.IsReadOnly = readonlyWebDav)
                .AddFilesystem(options => options.SourcePath = contentRoot);
        }
        
        builder.Services.AddHttpClient();
        var host = builder.Build();
        host.UseForwardedHeaders();
        if (allowDirectoryBrowsing)
        {
            host.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = new PhysicalFileProvider(contentRoot),
                Formatter = new SortedHtmlDirectoryFormatter(HtmlEncoder.Default),
            });
        }
        
        if (autoMirror is not null)
        {
            host.UseMiddleware<MirrorMiddleware>();
        }
        else
        {
            host.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "index.html", "index.htm" }
            });
        }
        
        if (enableWebDav)
        {
            var logger = host.Services.GetRequiredService<ILogger<StaticHandler>>();
            logger.LogInformation("WebDAV is enabled. Please open your WebDAV client and connect to the server using the following URL: http://localhost:{port}/webdav", port);
            host.UseWebDav(new PathString("/webdav"));
        }

        if (notFoundPage is not null)
        {
            host.UseMiddleware<NotFoundMiddleware>();
        }
        
        host.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true
        });
        
        return host;
    }
}