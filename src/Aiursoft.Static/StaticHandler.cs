using System.CommandLine;
using System.CommandLine.Invocation;
using Aiursoft.CommandFramework.Framework;

public class StaticHandler : ExecutableCommandHandlerBuilder
{
    public override string Name => "static";

    public override string Description => "Start a static file server.";
    
    public override Option[] GetCommandOptions() => new Option[]
    {
        OptionsProvider.PortOption,
        OptionsProvider.FolderOption,
    };

    protected override async Task Execute(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForOption(OptionsProvider.FolderOption)!;
        var port = context.ParseResult.GetValueForOption(OptionsProvider.PortOption);
        var app = BuildApp(path, port);
        await app.RunAsync();
    }

    private static WebApplication BuildApp(string path, int port)
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
        host.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = new List<string> { "index.html", "index.htm" }
        });
        host.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true
        });
        return host;
    }
}