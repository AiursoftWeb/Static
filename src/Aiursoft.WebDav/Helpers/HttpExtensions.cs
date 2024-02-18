using System.Web;
using Microsoft.AspNetCore.Http;

namespace Aiursoft.WebDav.Helpers
{
    public static class HttpExtensions
    {
        public static async Task<string> ReadContentAsString(this HttpRequest request)
        {
            var reader = new StreamReader(request.Body);

            var content = await reader.ReadToEndAsync();

            return content;
        }

        public static string UrlDecode(this string value)
        {
            return HttpUtility.UrlDecode(value);
        }
    }
}
