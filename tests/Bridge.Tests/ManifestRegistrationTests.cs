using Bridge.Abstractions;
using Bridge.Runtime;
using DotNetImpls;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bridge.Tests;

public class ManifestRegistrationTests
{
    [Fact]
    public void ShouldRegisterFromManifest()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();

        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");

        var provider = services.BuildServiceProvider();
        var textOps = provider.GetRequiredService<ITextOps>();

        textOps.Should().NotBeNull();
        textOps.Slugify("test").Should().Be("test");
    }

    [Fact]
    public void ShouldSwitchBetweenImplementations()
    {
        PyHost.Initialize();

        var pythonServices = new ServiceCollection();
        pythonServices.AddLogging();
        pythonServices.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        pythonServices.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");

        var dotnetServices = new ServiceCollection();
        dotnetServices.AddLogging();
        dotnetServices.AddScoped<ITextOps, TextOpsDotNet>();

        var pythonProvider = pythonServices.BuildServiceProvider();
        var dotnetProvider = dotnetServices.BuildServiceProvider();

        var pythonOps = pythonProvider.GetRequiredService<ITextOps>();
        var dotnetOps = dotnetProvider.GetRequiredService<ITextOps>();

        var pythonResult = pythonOps.Slugify("Hello World");
        var dotnetResult = dotnetOps.Slugify("Hello World");

        pythonResult.Should().Be(dotnetResult);
        pythonResult.Should().Be("hello-world");
    }
}
