using System.CommandLine;

namespace Aiursoft.Static;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = BuildCommand();
        await rootCommand.InvokeAsync(args);
    }

    public static RootCommand BuildCommand()
    {
        var fileOption = new Option<string>(
            name: "--path",
            getDefaultValue: () => ".",
            description: "The folder to start the server.");
        var portOption = new Option<int>(
            aliases: new [] { "--port", "-p" },
            getDefaultValue: () => 8080,
            description: "The port to listen for the server.");

        var rootCommand = new RootCommand("A simple static files HTTP server.");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(portOption);
        rootCommand.SetHandler(async (path, port) =>
        {
            var host = BuildApp(path, port);
            await host.RunAsync();
        }, fileOption, portOption);
        return rootCommand;
    }

    public static WebApplication BuildApp(string path, int port)
    {
        var contentRoot = Path.GetFullPath(path);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = contentRoot
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
