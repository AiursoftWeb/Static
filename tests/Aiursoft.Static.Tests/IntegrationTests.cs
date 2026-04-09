using System.Diagnostics;
using System.Net;
using System.Text;
using Aiursoft.CommandFramework;
using Aiursoft.CSTools.Tools;
using Aiursoft.Static.Handlers;
using WebDav;

[assembly: DoNotParallelize]

namespace Aiursoft.Static.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly SingleCommandApp<StaticHandler> _program = new();
    private string _indexContentFile = null!;

    [TestInitialize]
    public async Task ResetContentFolder()
    {
        _indexContentFile = Path.Combine(AppContext.BaseDirectory, "Assets", "index.html");
        await File.WriteAllTextAsync(_indexContentFile, "<h2>Hello world!</h2>");
    }

    [TestMethod]
    public async Task InvokeHelp()
    {
        var result = await _program.TestRunAsync(new[] { "--help" });
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeVersion()
    {
        var result = await _program.TestRunAsync(new[] { "--version" });
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeUnknown()
    {
        var result = await _program.TestRunAsync(new[] { "--wtf" });
        Assert.AreEqual(1, result.ProgramReturn);
    }

    private async Task<HttpResponseMessage> TestServer(string requestPath, params string[] options)
    {
        var availablePort = Network.GetAvailablePort();
        var basicArgs = CreateTestRunArguments(AppContext.BaseDirectory, availablePort, options);
        await Task.Factory.StartNew(() => _program.TestRunAsync(basicArgs));
        await Task.Delay(1000);

        var response = await new HttpClient().GetAsync($"http://localhost:{availablePort}{requestPath}");
        return response;

        string[] CreateTestRunArguments(string baseDirectory, int port, string[] extraOptions)
        {
            var args = new List<string>
            {
                "--path",
                Path.Combine(baseDirectory, "Assets"),
                "--port",
                port.ToString()
            };
            args.AddRange(extraOptions);
            return args.ToArray();
        }
    }

    [TestMethod]
    public async Task BasicServer()
    {
        var response = await TestServer("/");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("<h2>Hello world!</h2>", responseString);
    }

    [TestMethod]
    public async Task TestDirectoryBrowsing()
    {
        var response = await TestServer("/", "--allow-directory-browsing");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("index.html", responseString);
    }

    [TestMethod]
    public async Task TestMirrorWithoutCache()
    {
        var response = await TestServer("/", "--mirror", "https://www.baidu.com");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>", responseString);
    }

    [TestMethod]
    public async Task TestMirrorWithCache()
    {
        var response = await TestServer("/", "--mirror", "https://www.baidu.com", "--cache-mirror");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>", responseString);

        var cacheContent = await File.ReadAllTextAsync(_indexContentFile);
        Assert.Contains("<title>", cacheContent);
    }

    [TestMethod]
    public async Task TestWebDavReadonly()
    {
        var response = await TestServer("/webdav/index.html",
            "--enable-webdav");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hello world!", responseString);
    }

    [TestMethod]
    public async Task TestWebDav()
    {
        // Clean up
        if (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Assets")))
        {
            Directory.Delete(Path.Combine(AppContext.BaseDirectory, "Assets"), true);
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Assets"));
            await File.WriteAllTextAsync(_indexContentFile, "<h2>Hello world!</h2>");
        }

        var availablePort = Network.GetAvailablePort();
        await Task.Factory.StartNew(() => _program.TestRunAsync(
            new[]
            {
                "--path", Path.Combine(AppContext.BaseDirectory, "Assets"),
                "--port", availablePort.ToString(),
                "--enable-webdav", "--enable-webdav-write"
            }));
        await Task.Delay(1000);

        // Ensure server started
        var indexFile = await new HttpClient().GetAsync($"http://localhost:{availablePort}/index.html");
        Assert.AreEqual(HttpStatusCode.OK, indexFile.StatusCode);

        var clientParams = new WebDavClientParams { BaseAddress = new Uri($"http://localhost:{availablePort}/webdav/") };
        using var client = new WebDavClient(clientParams);

        // Put a file
        var newFileContent = "MyTestFile Content";
        await client.PutFile("file.txt", new MemoryStream(Encoding.UTF8.GetBytes(newFileContent)));

        // Download a file
        var response = await client.GetRawFile("file.txt");
        var responseString = await new StreamReader(response.Stream).ReadToEndAsync();
        Assert.AreEqual(newFileContent, responseString);

        // Create a folder
        var createFolderResult = await client.Mkcol("new-folder");
        Assert.AreEqual((int)HttpStatusCode.Created, createFolderResult.StatusCode);

        // Move a file
        var moveResult = await client.Move("file.txt", "new-folder/dest.txt");
        Assert.AreEqual((int)HttpStatusCode.Created, moveResult.StatusCode);

        // List folder content
        var list = await client.Propfind("new-folder");
        Assert.IsTrue(list.Resources.Any(f => f.Uri!.ToString().EndsWith("dest.txt")));

        // Move a folder
        var moveFolderResult = await client.Move("new-folder", "new-folder2");
        Assert.AreEqual((int)HttpStatusCode.Created, moveFolderResult.StatusCode);

        // List folder content
        list = await client.Propfind("new-folder2");
        Assert.IsTrue(list.Resources.Any(f => f.Uri!.ToString().EndsWith("dest.txt")));

        // Delete a folder
        var deleteResult = await client.Delete("new-folder2");
        Assert.AreEqual((int)HttpStatusCode.NoContent, deleteResult.StatusCode);

        // Put a file
        var putFileResult = await client.PutFile("file2.txt", new MemoryStream(Encoding.UTF8.GetBytes(newFileContent)));
        Assert.AreEqual((int)HttpStatusCode.Created, putFileResult.StatusCode);

        // Delete a file
        var deleteFileResult = await client.Delete("file2.txt");
        Assert.AreEqual((int)HttpStatusCode.NoContent, deleteFileResult.StatusCode);

        // List folder content
        list = await client.Propfind("");
        Assert.IsFalse(list.Resources.Any(f => f.Uri!.ToString().EndsWith("dest.txt")));
    }

    // -------------------------------------------------------------------------
    // Speed-limit tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Helper: Start the server, download a file, return (HTTP response, elapsed time).
    /// The content is read to completion so the stopwatch captures the full transfer.
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, byte[] Body, TimeSpan Elapsed)>
        DownloadWithTiming(string fileName, int fileSizeBytes, params string[] options)
    {
        // Write the test payload to disk.
        var filePath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        var payload = new byte[fileSizeBytes];
        // Fill with a recognisable pattern so corruption is detectable.
        for (var i = 0; i < fileSizeBytes; i++)
            payload[i] = (byte)(i % 251); // 251 is prime, gives a repeating pattern
        await File.WriteAllBytesAsync(filePath, payload);

        var availablePort = Network.GetAvailablePort();
        var args = new List<string>
        {
            "--path", Path.Combine(AppContext.BaseDirectory, "Assets"),
            "--port", availablePort.ToString()
        };
        args.AddRange(options);

        await Task.Factory.StartNew(() => _program.TestRunAsync(args.ToArray()));
        await Task.Delay(1000); // wait for Kestrel to start

        var sw = Stopwatch.StartNew();
        var response = await new HttpClient().GetAsync($"http://localhost:{availablePort}/{fileName}");
        var body = await response.Content.ReadAsByteArrayAsync();
        sw.Stop();

        return (response.StatusCode, body, sw.Elapsed);
    }

    [TestMethod]
    public async Task SpeedLimit_ThrottlesDownload()
    {
        // 512 KiB file, limited to 256 KiB/s → should take ≥ 1.5 s on any machine.
        const int fileSizeBytes = 512 * 1024;
        const int limitKbps = 256; // KiB/s

        var (statusCode, body, elapsed) = await DownloadWithTiming(
            "throttle_test.bin",
            fileSizeBytes,
            "--max-speed-kbps", limitKbps.ToString());

        Assert.AreEqual(HttpStatusCode.OK, statusCode);
        Assert.AreEqual(fileSizeBytes, body.Length, "Response body was truncated or padded.");

        // At 256 KiB/s, 512 KiB should take at least 1.5 s (generous lower bound).
        Assert.IsTrue(
            elapsed.TotalSeconds >= 1.5,
            $"Expected download to take ≥ 1.5 s, but it finished in {elapsed.TotalSeconds:F2} s. Speed limit may not be applied.");
    }

    [TestMethod]
    public async Task SpeedLimit_ContentIsNotCorrupted()
    {
        // 256 KiB at 128 KiB/s – verifies every byte survives the throttled path.
        const int fileSizeBytes = 256 * 1024;
        const int limitKbps = 128;

        var (statusCode, body, _) = await DownloadWithTiming(
            "integrity_test.bin",
            fileSizeBytes,
            "--max-speed-kbps", limitKbps.ToString());

        Assert.AreEqual(HttpStatusCode.OK, statusCode);
        Assert.AreEqual(fileSizeBytes, body.Length);

        // Verify the byte pattern is intact.
        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] != (byte)(i % 251))
                Assert.Fail($"Content corrupted at byte offset {i}: expected {i % 251}, got {body[i]}.");
        }
    }

    [TestMethod]
    public async Task SpeedLimit_ZeroMeansUnlimited()
    {
        // 512 KiB with --max-speed-kbps 0 (the default) should finish well under 1 s on loopback.
        const int fileSizeBytes = 512 * 1024;

        var (statusCode, body, elapsed) = await DownloadWithTiming(
            "unlimited_test.bin",
            fileSizeBytes,
            "--max-speed-kbps", "0");

        Assert.AreEqual(HttpStatusCode.OK, statusCode);
        Assert.AreEqual(fileSizeBytes, body.Length);

        Assert.IsTrue(
            elapsed.TotalSeconds < 3.0,
            $"Unlimited download of {fileSizeBytes / 1024} KiB took {elapsed.TotalSeconds:F2} s – suspiciously slow.");
    }
}
