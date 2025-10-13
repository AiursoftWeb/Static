using Aiursoft.Static.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.Static.Middlewares;

public class MirrorMiddleware(
    IOptions<MirrorConfiguration> options,
    RequestDelegate next,
    ILogger<WebApplication> logger,
    HttpClient client)
{
    private readonly MirrorConfiguration _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        if (context.Response.StatusCode != 404 || context.Request.Method != "GET")
        {
            return;
        }

        logger.LogWarning("404: {RequestPath}, but enabled auto mirror. Will try to mirror the file.", context.Request.Path);
        var requestPath = context.Request.Path.Value;
        var mirrorPath = _options.MirrorWebSite + requestPath;

        try
        {
            // Set up a cancellation token with the configured timeout.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1200));

            // Pass the token to the GetAsync method.
            var mirrorResponse = await client.GetAsync(mirrorPath, cts.Token);

            if (mirrorResponse.IsSuccessStatusCode)
            {
                logger.LogTrace("File: {MirrorPath} can be mirrored.", mirrorPath);
                var contentType = mirrorResponse.Content.Headers.ContentType?.MediaType;
                var content = await mirrorResponse.Content.ReadAsByteArrayAsync(cts.Token);
                context.Response.StatusCode = 200;
                context.Response.ContentType = contentType;
                await context.Response.Body.WriteAsync(content, cts.Token);
                logger.LogInformation("Mirrored file done successfully: {MirrorPath}", mirrorPath);

                if (_options.CachedMirroredFiles)
                {
                    await CacheMirroredFileAsync(context, requestPath, content);
                }
            }
        }
        // This exception is thrown when the timeout is reached.
        catch (OperationCanceledException)
        {
            logger.LogWarning("Mirroring timed out for {MirrorPath} after {Timeout} seconds.", mirrorPath, 1200);
            // Do nothing here. The response status code remains 404, which is correct.
        }
        catch (Exception ex)
        {
            // Catch other potential exceptions (e.g., DNS errors) for robust logging.
            logger.LogError(ex, "An unexpected error occurred while trying to mirror {MirrorPath}.", mirrorPath);
        }
    }

    private async Task CacheMirroredFileAsync(HttpContext context, string? requestPath, byte[] content)
    {
        if (string.IsNullOrEmpty(requestPath)) return;

        logger.LogTrace("The mirrored file: {MirrorPath} can be cached.", _options.MirrorWebSite + requestPath);
        var requestFilePath = requestPath.Split('?')[0];
        var contentRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

        // If no extension, assume it's a directory and append index.html
        if (Path.GetExtension(requestFilePath) == string.Empty)
        {
            requestFilePath = Path.Combine(requestFilePath, "index.html");
        }

        var filePath = Path.Combine(contentRoot, requestFilePath.TrimStart('/'));
        var file = new FileInfo(filePath);
        file.Directory?.Create();
        await File.WriteAllBytesAsync(file.FullName, content);
        logger.LogInformation("Cached file done successfully: {FilePath}", filePath);
    }
}
