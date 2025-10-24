using Bridge.Abstractions;
using Bridge.Runtime;
using Bridge.Runtime.Converters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using Xunit;

namespace Bridge.Tests;

public class PythonProxyTests
{
    [Fact]
    public void ShouldInvokeSyncMethod()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");

        var provider = services.BuildServiceProvider();
        var textOps = provider.GetRequiredService<ITextOps>();

        var result = textOps.Slugify("Hello World Test");
        result.Should().Be("hello-world-test");
    }

    [Fact]
    public async Task ShouldInvokeAsyncMethod()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");

        var provider = services.BuildServiceProvider();
        var textOps = provider.GetRequiredService<ITextOps>();

        var result = await textOps.SummarizeAsync("This is a very long text that should be summarized because it exceeds the maximum length limit");
        result.Should().Be("This is a very long text that should be summarized because it exceeds the maximum length limit...");
    }

    [Fact]
    public void ShouldHandleScopedLifetime()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps", lifetime: ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var textOps1 = scope1.ServiceProvider.GetRequiredService<ITextOps>();
        var textOps2 = scope2.ServiceProvider.GetRequiredService<ITextOps>();

        textOps1.Should().NotBeSameAs(textOps2);

        var result1 = textOps1.Slugify("test1");
        var result2 = textOps2.Slugify("test2");

        result1.Should().Be("test1");
        result2.Should().Be("test2");
    }
}
