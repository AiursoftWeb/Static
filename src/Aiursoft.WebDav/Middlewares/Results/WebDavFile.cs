using System.Xml.Linq;

namespace Aiursoft.WebDav.Middlewares.Results
{
    /// <summary>
    /// WebDavFile
    /// </summary>
    public class WebDavFile : WebDavXmlResult
    {
        public WebDavFile(FileInfo file)
        {
            DisplayName = file.Name;
            Length = file.Length;
            CreatedAt = file.CreationTime;
            ModifiedAt = file.LastWriteTime;
        }

        public string DisplayName { get; set; }

        public long Length { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public override XElement ToXml(WebDavContext context)
        {
            return new XElement(dav + "response",
                        new XElement(dav + "href", $"{context.BaseUrl}/{DisplayName}"),
                        new XElement(dav + "propstat",
                            new XElement(dav + "prop",
                             new XElement(dav + "displayname", DisplayName),
                             new XElement(dav + "getcontentlength", $"{Length}"),
                             new XElement(dav + "creationdate", $"{CreatedAt:yyyy-MM-ddTHH:mm:sszzz}"),
                             new XElement(dav + "getlastmodified", $"{ModifiedAt:yyyy-MM-ddTHH:mm:sszzz}")
                             ))
                );
        }
    }
}
