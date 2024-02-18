using System.Net;

namespace Aiursoft.WebDav.Middlewares.Results
{
    public class WebDavNoContentResult : IWebDavResult
    {
        public WebDavNoContentResult(HttpStatusCode statusCode)
        {
            StatusCode = (int)statusCode;
        }

        public virtual int StatusCode { get; }
    }
}
