using Aiursoft.Static.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.Static.Middlewares;

public class MirrorMiddleware
{
    private readonly MirrorConfiguration _options;
    private readonly HttpClient _client;
    private readonly RequestDelegate _next;
    private readonly ILogger<WebApplication> _logger;

    public MirrorMiddleware(
        IOptions<MirrorConfiguration> options,
        RequestDelegate next, 
        ILogger<WebApplication> logger,
        HttpClient client)
    {
        _options = options.Value;
        _client = client;
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        if (context.Response.StatusCode == 404 && context.Request.Method == "GET")
        {
            _logger.LogWarning($"404: {context.Request.Path}, but enabled auto mirror. Will try to mirror the file.");
            var requestPath = context.Request.Path.Value;
            var mirrorPath = _options.MirrorWebSite + requestPath;
            var mirrorResponse = await _client.GetAsync(mirrorPath);
            if (mirrorResponse.IsSuccessStatusCode)
            {
                _logger.LogTrace($"File: {mirrorPath} can be mirrored.");
                var contentType = mirrorResponse.Content.Headers.ContentType?.MediaType;
                var content = await mirrorResponse.Content.ReadAsByteArrayAsync();
                context.Response.StatusCode = 200;
                context.Response.ContentType = contentType;
                await context.Response.Body.WriteAsync(content);
                _logger.LogInformation($"Mirrored file done successfully: {mirrorPath}");
                
                if (_options.CachedMirroredFiles)
                {
                    _logger.LogTrace($"The mirrored file: {mirrorPath} can be cached.");
                    var requestFilePath = requestPath!.Split('?')[0];
                    var contentRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
                    
                    // If no extension, assume it's a directory and append index.html
                    if (Path.GetExtension(requestFilePath) == string.Empty)
                    {
                        requestFilePath += "/index.html";
                    }
                    var filePath = Path.Combine(contentRoot, requestFilePath.TrimStart('/'));
                    var file = new FileInfo(filePath);
                    file.Directory?.Create();
                    await File.WriteAllBytesAsync(file.FullName, content);
                    _logger.LogInformation($"Cached file done successfully: {filePath}");
                }
            }
        }
       
    }
}