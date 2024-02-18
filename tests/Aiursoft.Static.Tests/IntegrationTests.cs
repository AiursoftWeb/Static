﻿using System.Net;
using System.Text;
using Aiursoft.CommandFramework;
using Aiursoft.CSTools.Tools;
using Aiursoft.Static.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebDav;

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
        Assert.IsTrue(responseString.Contains("index.html"));
    }

    [TestMethod]
    public async Task TestMirrorWithoutCache()
    {
        var response = await TestServer("/", "--mirror", "https://www.aiursoft.cn");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(responseString.Contains("Aiursoft"));
    }

    [TestMethod]
    public async Task TestMirrorWithCache()
    {
        var response = await TestServer("/", "--mirror", "https://www.aiursoft.cn", "--cache-mirror");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(responseString.Contains("Aiursoft"));

        var cacheContent = await File.ReadAllTextAsync(_indexContentFile);
        Assert.IsTrue(cacheContent.Contains("Aiursoft"));
    }

    [TestMethod]
    public async Task TestWebDavReadonly()
    {
        var response = await TestServer("/webdav/index.html",
            "--enable-webdav");
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(responseString.Contains("Hello world!"));
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
        
        // Move a file
        var moveResult = await client.Move("file.txt", "dest.txt");
        Assert.AreEqual((int)HttpStatusCode.Created, moveResult.StatusCode);
        
        // List folder content
        var list = await client.Propfind("");
        Assert.IsTrue(list.Resources.Any(f => f.Uri.ToString().EndsWith("dest.txt")));
        
        // Delete a file
        await client.Delete("dest.txt");
        
        // List folder content
        list = await client.Propfind("/");
        Assert.IsFalse(list.Resources.Any(f => f.Uri.ToString().EndsWith("dest.txt")));
    }
}