using Aiursoft.Static.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.Static.Middlewares;

public class NotFoundMiddleware(RequestDelegate _next, IOptions<NotFoundConfiguration> options)
{
    private readonly NotFoundConfiguration _options = options.Value; 
    
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        if (context.Response.StatusCode == 404)
        {
            var origPath = context.Request.Path;
            context.Request.Path = _options.NotFoundPage!;
            await _next(context);
            context.Request.Path = origPath; // For correct logging and middleware compatibility
        }
    }
}