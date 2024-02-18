using Microsoft.AspNetCore.Http;

namespace Aiursoft.WebDav.Middlewares
{
    public static class HttpContextExtensions
    {
        public static WebDavContext GetWebDavContext(this HttpContext httpContent)
        {
            var webDavContext = (WebDavContext?)httpContent.Items["WebDavContext"];

            if (webDavContext == null)
            {
                throw new Exception("WebDAV context was not found.");
            }

            return webDavContext;
        }

        public static void SetWebDavContext(this HttpContext httpContext, WebDavContext webDavContext)
        {
            httpContext.Items.Add("WebDavContext", webDavContext);
        }
    }
}
