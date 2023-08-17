using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [TestMethod]
        public async Task CallServer()
        {
            var http = new HttpClient();
            var result = await http.GetAsync("http://localhost:8080");
            var content = await result.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("Hello"));
        }
    }
}