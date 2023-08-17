using System.CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Aiursoft.Static.Tests;

[TestClass]
public class CommandTests
{
    private readonly RootCommand _program;

    public CommandTests()
    {
        _program = Program.BuildCommand();
    }

    [TestMethod]
    public async Task InvokeHelp()
    {
        var result = await _program.InvokeAsync(new[] { "--help" });
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task InvokeVersion()
    {
        var result = await _program.InvokeAsync(new[] { "--version" });
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task InvokeUnknown()
    {
        var result = await _program.InvokeAsync(new[] { "--wtf" });
        Assert.AreEqual(1, result);
    }
}
