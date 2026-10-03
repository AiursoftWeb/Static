using System.Net;
using System.Text;
using Aiursoft.Static.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Static.Tests;

[TestClass]
public class MirrorCacheTests
{
    private string _root = null!;
    private WebApplication _upstream = null!;
    private WebApplication? _mirror;
    private HttpClient? _client;
    private int _upstreamRequests;
    private Func<HttpContext, Task> _respond = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _root = Directory.CreateTempSubdirectory("static-mirror-tests-").FullName;
        _respond = context => context.Response.WriteAsync("signed repository metadata");
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        _upstream = builder.Build();
        _upstream.Run(async context =>
        {
            Interlocked.Increment(ref _upstreamRequests);
            await _respond(context);
        });
        await _upstream.StartAsync();
    }

    private async Task StartMirror(bool cache = true)
    {
        _mirror = StaticHandler.BuildApp(_root, 0, false, _upstream.Urls.Single(),
            cache, false, false, null);
        await _mirror.StartAsync();
        var port = new Uri(_mirror.Urls.Single()).Port;
        _client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();
        if (_mirror is not null)
        {
            await _mirror.StopAsync();
            await _mirror.DisposeAsync();
        }
        await _upstream.StopAsync();
        await _upstream.DisposeAsync();
        Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    [DataRow("ubuntu/dists/resolute/InRelease")]
    [DataRow("ubuntu/dists/resolute/Release")]
    [DataRow("ubuntu/dists/resolute/Release.gpg")]
    [DataRow("ubuntu/dists/resolute/main/binary-amd64/Packages.xz")]
    [DataRow("ubuntu/dists/resolute/main/binary-amd64/by-hash/SHA256/abcdef0123456789")]
    public async Task CachedFileWorksOfflineAndSupportsConditionalRequests(string path)
    {
        await StartMirror();
        using var first = await _client!.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        var expected = await first.Content.ReadAsByteArrayAsync();
        CollectionAssert.AreEqual(expected, await File.ReadAllBytesAsync(Path.Combine(_root, path)));
        Assert.AreEqual(1, _upstreamRequests);

        await _upstream.StopAsync();
        using var cached = await _client.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, cached.StatusCode);
        CollectionAssert.AreEqual(expected, await cached.Content.ReadAsByteArrayAsync());
        Assert.IsNotNull(cached.Headers.ETag);
        Assert.IsNotNull(cached.Content.Headers.LastModified);

        using var etagRequest = new HttpRequestMessage(HttpMethod.Get, path);
        etagRequest.Headers.IfNoneMatch.Add(cached.Headers.ETag);
        using var etagResponse = await _client.SendAsync(etagRequest);
        Assert.AreEqual(HttpStatusCode.NotModified, etagResponse.StatusCode);
        Assert.AreEqual(0, (await etagResponse.Content.ReadAsByteArrayAsync()).Length);

        using var modifiedRequest = new HttpRequestMessage(HttpMethod.Get, path);
        modifiedRequest.Headers.IfModifiedSince = cached.Content.Headers.LastModified;
        using var modifiedResponse = await _client.SendAsync(modifiedRequest);
        Assert.AreEqual(HttpStatusCode.NotModified, modifiedResponse.StatusCode);

        using var headRequest = new HttpRequestMessage(HttpMethod.Head, path);
        using var headResponse = await _client.SendAsync(headRequest);
        Assert.AreEqual(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.AreEqual(expected.Length, headResponse.Content.Headers.ContentLength);
        Assert.AreEqual(1, _upstreamRequests, "Cache hits must not contact the upstream.");
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/directory/")]
    [DataRow("/directory.with.dots/")]
    public async Task TrailingSlashStoresIndexHtml(string path)
    {
        await StartMirror();
        var body = await _client!.GetStringAsync(path);
        var cachedPath = Path.Combine(_root, path.TrimStart('/'), "index.html");
        Assert.AreEqual(body, await File.ReadAllTextAsync(cachedPath));
    }

    [TestMethod]
    public async Task CacheDisabledFetchesEveryTimeWithoutWritingFiles()
    {
        await StartMirror(cache: false);
        Assert.AreEqual(await _client!.GetStringAsync("InRelease"),
            await _client.GetStringAsync("InRelease"));
        Assert.AreEqual(2, _upstreamRequests);
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_root).Length);
    }

    [TestMethod]
    public async Task LegacyDirectoryIsPreservedAndDoesNotBreakResponse()
    {
        var legacy = Directory.CreateDirectory(Path.Combine(_root, "InRelease"));
        var oldFile = Path.Combine(legacy.FullName, "index.html");
        await File.WriteAllTextAsync(oldFile, "old cached data");
        await StartMirror();
        Assert.AreEqual("signed repository metadata", await _client!.GetStringAsync("InRelease"));
        Assert.AreEqual("old cached data", await File.ReadAllTextAsync(oldFile));
        Assert.AreEqual(0, Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task UpstreamNotFoundIsNotCached()
    {
        _respond = context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        };
        await StartMirror();
        using var response = await _client!.GetAsync("InRelease");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_root).Length);
    }

    [TestMethod]
    public async Task ConcurrentDownloadsPublishCompleteFilesAndLeaveNoTemporaryFiles()
    {
        var payload = Encoding.UTF8.GetBytes(new string('a', 512 * 1024));
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        const int clients = 4;
        var started = 0;
        _respond = async context =>
        {
            await context.Response.Body.WriteAsync(payload.AsMemory(0, payload.Length / 2));
            await context.Response.Body.FlushAsync();
            if (Interlocked.Increment(ref started) == clients)
            {
                allStarted.TrySetResult();
            }
            await release.Task;
            await context.Response.Body.WriteAsync(payload.AsMemory(payload.Length / 2));
        };
        await StartMirror();
        var downloads = Enumerable.Range(0, clients)
            .Select(_ => _client!.GetByteArrayAsync("Packages.xz")).ToArray();
        try
        {
            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsFalse(File.Exists(Path.Combine(_root, "Packages.xz")),
                "An incomplete upstream download must not become a cache hit.");
        }
        finally
        {
            release.TrySetResult();
        }
        foreach (var body in await Task.WhenAll(downloads))
        {
            CollectionAssert.AreEqual(payload, body);
        }
        CollectionAssert.AreEqual(payload, await File.ReadAllBytesAsync(Path.Combine(_root, "Packages.xz")));
        Assert.AreEqual(1, Directory.GetFiles(_root).Length, "Temporary files must be cleaned up.");
    }
}
