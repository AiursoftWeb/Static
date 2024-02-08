using System.CommandLine;
using System.CommandLine.Invocation;
using Aiursoft.CommandFramework.Framework;

namespace Aiursoft.Static;

public class StaticHandler : ExecutableCommandHandlerBuilder
{
    protected override string Name => "static";

    protected override string Description => "Start a static file server.";
    
    protected override Option[] GetCommandOptions() => new Option[]
    {
        OptionsProvider.PortOption,
        OptionsProvider.FolderOption,
        OptionsProvider.AllowDirectoryBrowsingOption,
        OptionsProvider.MirrorWebSiteOption,
        OptionsProvider.CachedMirroredFilesOption
    };

    protected override async Task Execute(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForOption(OptionsProvider.FolderOption)!;
        var port = context.ParseResult.GetValueForOption(OptionsProvider.PortOption);
        var allowDirectoryBrowsing = context.ParseResult.GetValueForOption(OptionsProvider.AllowDirectoryBrowsingOption);
        var autoMirror = context.ParseResult.GetValueForOption(OptionsProvider.MirrorWebSiteOption);
        var cacheMirror = context.ParseResult.GetValueForOption(OptionsProvider.CachedMirroredFilesOption);
        
        var app = BuildApp(path, port, allowDirectoryBrowsing, autoMirror, cacheMirror);
        await app.RunAsync();
    }

    private static WebApplication BuildApp(string path, int port, bool allowDirectoryBrowsing, string? autoMirror, bool cacheMirror)
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
        var host = builder.Build();
        if (allowDirectoryBrowsing)
        {
            host.UseDirectoryBrowser();
        }
        
        host.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = new List<string> { "index.html", "index.htm" }
        });
        if (autoMirror is not null)
        {
            host.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404 && context.Request.Method == "GET")
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<WebApplication>>();
                    logger.LogWarning($"404: {context.Request.Path}, but enabled auto mirror. Will try to mirror the file.");
                    
                    var requestPath = context.Request.Path.Value;
                    var mirrorPath = autoMirror + requestPath;
                    var client = new HttpClient();
                    var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
                    if (userAgent is not null)
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                    }
                    var mirrorResponse = await client.GetAsync(mirrorPath);
                    if (mirrorResponse.IsSuccessStatusCode)
                    {
                        var contentType = mirrorResponse.Content.Headers.ContentType?.MediaType;
                        var content = await mirrorResponse.Content.ReadAsByteArrayAsync();
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = contentType;
                        await context.Response.Body.WriteAsync(content);

                        if (cacheMirror)
                        {
                            var requestFilePath = requestPath!.Split('?')[0];
                            if (requestFilePath.EndsWith('/'))
                            {
                                requestFilePath += "index.html";
                            }
                            var filePath = Path.Combine(contentRoot, requestFilePath.TrimStart('/'));

                            
                            var file = new FileInfo(filePath);
                            file.Directory?.Create();
                            await File.WriteAllBytesAsync(file.FullName, content);
                        }
                    }
                }
            });
        }
        
        host.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true
        });
        return host;
    }
}