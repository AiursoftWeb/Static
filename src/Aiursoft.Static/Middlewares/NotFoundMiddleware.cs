namespace Aiursoft.Static.Middlewares;

public class NotFoundMiddleware(RequestDelegate _next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        if (context.Response.StatusCode == 404)
        {
            var origPath = context.Request.Path;
            context.Request.Path = "/404.html";
            await _next(context);
            context.Request.Path = origPath; // For correct logging and middleware compatibility
        }
    }
}