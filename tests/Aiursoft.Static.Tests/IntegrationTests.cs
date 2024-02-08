using Aiursoft.CommandFramework;
using Aiursoft.Static.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.Static.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly SingleCommandApp<StaticHandler> _program = new();

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
}