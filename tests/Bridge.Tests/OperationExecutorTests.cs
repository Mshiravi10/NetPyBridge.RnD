using Bridge.Abstractions;
using Bridge.Runtime;
using Bridge.Runner;
using Bridge.Runner.Models;
using DotNetImpls;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bridge.Tests;

public class OperationExecutorTests
{
    [Fact]
    public async Task ShouldExecuteOperation()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");
        services.AddScoped<OperationExecutor>();

        var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<OperationExecutor>();

        var operation = new OperationCall
        {
            Interface = "ITextOps",
            Method = "Slugify",
            Args = new Dictionary<string, object> { ["input"] = "Hello World Test" },
            ResultKey = "slug"
        };

        var result = await executor.ExecuteAsync(operation);
        result.Should().Be("hello-world-test");
    }

    [Fact]
    public async Task ShouldExecuteAsyncOperation()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");
        services.AddScoped<OperationExecutor>();

        var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<OperationExecutor>();

        var operation = new OperationCall
        {
            Interface = "ITextOps",
            Method = "SummarizeAsync",
            Args = new Dictionary<string, object> { ["text"] = "This is a very long text that should be summarized" },
            ResultKey = "summary"
        };

        var result = await executor.ExecuteAsync(operation);
        result.Should().Be("This is a very long text that should be summarized");
    }

    [Fact]
    public async Task ShouldThrowOnInvalidInterface()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<OperationExecutor>();

        var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<OperationExecutor>();

        var operation = new OperationCall
        {
            Interface = "INonExistent",
            Method = "SomeMethod",
            Args = new Dictionary<string, object>(),
            ResultKey = "result"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(operation));
    }
}
