using System.Xml.Linq;

namespace Aiursoft.WebDav.Middlewares
{
    public interface IWebDavXmlResult : IWebDavResult
    {
        XElement ToXml(WebDavContext context);
    }
}
