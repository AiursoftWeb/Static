using System.Net;
using System.Xml.Linq;

namespace Aiursoft.WebDav.Middlewares.Results
{
    public class WebDavFileNotFoundResult : WebDavXmlResult
    {
        public override int StatusCode => (int)HttpStatusCode.NotFound;

        public override XElement ToXml(WebDavContext context)
        {
            return new XElement("NotFound");
        }
    }
}
