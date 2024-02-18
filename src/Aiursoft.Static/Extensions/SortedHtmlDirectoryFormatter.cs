using System.Text.Encodings.Web;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Aiursoft.Static.Extensions;

public class SortedHtmlDirectoryFormatter : HtmlDirectoryFormatter
{
    public SortedHtmlDirectoryFormatter(HtmlEncoder encoder) : base(encoder) { }

    public override Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents)
    {
        // Order folders first, then files
        // Then order by name
        var sorted = contents.OrderBy(x => !x.IsDirectory).ThenBy(x => x.Name);
        return base.GenerateContentAsync(context, sorted);
    }
}