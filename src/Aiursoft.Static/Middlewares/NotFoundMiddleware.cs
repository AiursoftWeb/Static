using Aiursoft.Static.Models.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.Static.Middlewares;

public class NotFoundMiddleware(RequestDelegate next, IOptions<NotFoundConfiguration> options)
{
    private readonly NotFoundConfiguration _options = options.Value; 
    
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        if (context.Response.StatusCode == 404 && context.Request.Method == "GET")
        {
            var origPath = context.Request.Path;
            context.Request.Path = _options.NotFoundPage!;
            await next(context);
            context.Request.Path = origPath; // For correct logging and middleware compatibility
        }
    }
}