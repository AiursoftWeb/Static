using System.CommandLine;

namespace Aiursoft.Static;

public static class OptionsProvider
{
    public static readonly Option<int> PortOption = new(
        aliases: new [] { "--port", "-p" },
        getDefaultValue: () => 8080,
        description: "The port to listen for the server.");
    
    public static readonly Option<string> FolderOption = new (
        name: "--path",
        getDefaultValue: () => ".",
        description: "The folder to start the server.");
    
    public static readonly Option<bool> AllowDirectoryBrowsingOption = new (
        name: "--allow-directory-browsing",
        getDefaultValue: () => false,
        description: "Allow directory browsing the server files under the path. This options if conflict with --mirror.");
    
    public static readonly Option<string?> MirrorWebSiteOption = new (
        name: "--mirror",
        description: "The website to mirror. Automatically proxy the file if the file is not found in the server. This option if conflict with --allow-directory-browsing.");
    
    public static readonly Option<bool> CachedMirroredFilesOption = new (
        name: "--cache-mirror",
        getDefaultValue: () => true,
        description: "Cache the mirrored files. This will save the mirrored files to the server's disk.");
}