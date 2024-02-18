using System.Text;

namespace Aiursoft.Static.Middlewares;

public class WebDavMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _rootPath;

    public WebDavMiddleware(RequestDelegate next, string rootPath)
    {
        _next = next;
        _rootPath = rootPath;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers.Add("DAV", "1,2");
            context.Response.Headers.Add("MS-Author-Via", "DAV");
            
            // Readonly WebDAV server.
            context.Response.Headers.Add("Allow", "GET, HEAD, PROPFIND, OPTIONS");
            context.Response.Headers.Add("Content-Length", "0");
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        if (context.Request.Method == "PROPFIND")
        {
            var path = context.Request.Path.Value ?? "/";
            var fullPath = Path.Combine(_rootPath, path);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var response = new StringBuilder();
            response.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            response.AppendLine("<D:multistatus xmlns:D=\"DAV:\">");
            response.AppendLine("  <D:response>");
            response.AppendLine("    <D:href>" + path + "</D:href>");
            response.AppendLine("    <D:propstat>");
            response.AppendLine("      <D:prop>");
            response.AppendLine("        <D:creationdate>" + File.GetCreationTime(fullPath).ToString("yyyy-MM-ddTHH:mm:ssZ") + "</D:creationdate>");
            response.AppendLine("        <D:getlastmodified>" + File.GetLastWriteTime(fullPath).ToString("yyyy-MM-ddTHH:mm:ssZ") + "</D:getlastmodified>");
            response.AppendLine("        <D:getcontentlength>" + new FileInfo(fullPath).Length + "</D:getcontentlength>");
            response.AppendLine("        <D:resourcetype>" + (File.Exists(fullPath) ? "" : "<D:collection/>") + "</D:resourcetype>");
            response.AppendLine("      </D:prop>");
            response.AppendLine("      <D:status>HTTP/1.1 200 OK</D:status>");
            response.AppendLine("    </D:propstat>");
            response.AppendLine("  </D:response>");
            response.AppendLine("</D:multistatus>");
            context.Response.ContentType = "text/xml";
            context.Response.StatusCode = StatusCodes.Status207MultiStatus;
            await context.Response.WriteAsync(response.ToString());
            return;
        }
        
        await _next(context);
        
        // No put, because this is a read-only WebDAV server.
    }
}