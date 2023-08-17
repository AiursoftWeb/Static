using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Aiursoft.Static.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        public string _testPath;
        public WebApplication _server;
        public IntegrationTests()
        {
            _testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            _server = Program.BuildApp(_testPath, 8080);
        }

        [TestInitialize]
        public async Task TestInit()
        {
            await _server.StartAsync();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await _server.StopAsync();
        }

        [TestMethod]
        public async Task CallServerRoot()
        {
            var http = new HttpClient();
            var result = await http.GetAsync("http://localhost:8080");
            var content = await result.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("Hello"));
        }

        [TestMethod]
        public async Task CallServerIndex()
        {
            var http = new HttpClient();
            var result = await http.GetAsync("http://localhost:8080/index.html");
            var content = await result.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("Hello"));
        }

        [TestMethod]
        public async Task CallServer404()
        {
            var http = new HttpClient();
            var result = await http.GetAsync("http://localhost:8080/404.html");
            Assert.AreEqual((HttpStatusCode)404, result.StatusCode);
        }
    }
}