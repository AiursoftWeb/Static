using System.CommandLine;

namespace Aiursoft.Static;

public static class OptionsProvider
{
    public static readonly Option<int> PortOption = new(
        name: "--port",
        aliases: ["-p"])
    {
        DefaultValueFactory = _ => 8080,
        Description = "The port to listen for the server."
    };

    public static readonly Option<string> PathOption = new (
        name: "--path")
    {
        DefaultValueFactory = _ => ".",
        Description = "The path to start the server."
    };

    public static readonly Option<bool> AllowDirectoryBrowsingOption = new (
        name: "--allow-directory-browsing")
    {
        DefaultValueFactory = _ => false,
        Description = "Allow directory browsing the server files under the path. This options if conflict with --mirror."
    };

    public static readonly Option<string?> MirrorWebSiteOption = new (
        name: "--mirror")
    {
        DefaultValueFactory = _ => null,
        Description = "The website to mirror. Automatically proxy the file if the file is not found in the server. This option if conflict with --allow-directory-browsing."
    };

    public static readonly Option<bool> CachedMirroredFilesOption = new (
        name: "--cache-mirror")
    {
        DefaultValueFactory = _ => false,
        Description = "Cache the mirrored files. This will save the mirrored files to the server's disk."
    };

    public static readonly Option<bool> EnableWebDavOption = new (
        name: "--enable-webdav")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable WebDAV for the server. This is a read-only WebDAV server."
    };

    public static readonly Option<bool> WebDavCanWriteOption = new (
        name: "--enable-webdav-write")
    {
        DefaultValueFactory = _ => false,
        Description = "Enable write access for the WebDAV server. This will allow the client to write files to the server. However, this requires the server process to run with write permission."
    };

    public static readonly Option<string?> NotFoundPageOption = new (
        name: "--not-found-page")
    {
        DefaultValueFactory = _ => null,
        Description = "Specifies a custom 404 page to be served when a requested file is not found. This file should reside in the server's root folder. If this option is left blank or not set, a default 404 response will be returned."
    };
}
