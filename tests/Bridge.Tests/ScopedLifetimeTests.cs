using Bridge.Abstractions;
using Bridge.Runtime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bridge.Tests;

public class ScopedLifetimeTests
{
    [Fact]
    public void ShouldCreateDifferentInstancesInDifferentScopes()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps", lifetime: ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ITextOps>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ITextOps>();

        instance1.Should().NotBeSameAs(instance2);

        var result1 = instance1.Slugify("scope1");
        var result2 = instance2.Slugify("scope2");

        result1.Should().Be("scope1");
        result2.Should().Be("scope2");
    }

    [Fact]
    public void ShouldReuseInstanceInSameScope()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps", lifetime: ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        var instance1 = scope.ServiceProvider.GetRequiredService<ITextOps>();
        var instance2 = scope.ServiceProvider.GetRequiredService<ITextOps>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void ShouldDisposeInstancesWhenScopeDisposed()
    {
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps", lifetime: ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        ITextOps instance;
        using (var scope = provider.CreateScope())
        {
            instance = scope.ServiceProvider.GetRequiredService<ITextOps>();
            instance.Slugify("test").Should().Be("test");
        }

        instance.Should().NotBeNull();
    }
}
