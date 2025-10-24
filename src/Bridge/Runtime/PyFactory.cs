using System.Reflection;
using Bridge.Abstractions;
using Bridge.Runtime.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace Bridge.Runtime;

public static class PyFactory
{
    public static IServiceCollection AddPythonBridge<TInterface>(
        this IServiceCollection services,
        string moduleName,
        string className,
        Func<IServiceProvider, object[]>? ctorArgsFactory = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class
    {
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            provider => CreateProxy<TInterface>(provider, moduleName, className, ctorArgsFactory),
            lifetime));

        return services;
    }

    private static TInterface CreateProxy<TInterface>(
        IServiceProvider provider,
        string moduleName,
        string className,
        Func<IServiceProvider, object[]>? ctorArgsFactory)
        where TInterface : class
    {
        var logger = provider.GetRequiredService<ILogger<PythonProxy<TInterface>>>();
        var asyncStrategy = provider.GetService<IPythonAsyncStrategy>() ?? new SimpleAsyncStrategy();

        var errorMode = GetErrorHandlingMode(provider);

        var proxy = DispatchProxy.Create<TInterface, PythonProxy<TInterface>>() as PythonProxy<TInterface>;
        if (proxy == null)
            throw new InvalidOperationException("Failed to create proxy");

        using (Py.GIL())
        {
            dynamic module = Py.Import(moduleName);
            dynamic pyClass = module.GetAttr(className);

            var ctorArgs = ctorArgsFactory?.Invoke(provider) ?? Array.Empty<object>();
            var pyArgs = ctorArgs.Select(PrimitiveConverter.ToPython).ToArray();

            dynamic pyInstance = pyArgs.Length == 0 ? pyClass() : pyClass(pyArgs);

            proxy.Initialize(pyInstance, logger, asyncStrategy, errorMode);

            return (TInterface)(object)proxy;
        }
    }

    private static ErrorHandlingMode GetErrorHandlingMode(IServiceProvider provider)
    {
        var config = provider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
        var envValue = Environment.GetEnvironmentVariable("NETPYBRIDGE_ERROR_MODE");

        if (Enum.TryParse<ErrorHandlingMode>(envValue, true, out var mode))
        {
            return mode;
        }

        var configValue = config?["ErrorHandling:Mode"];
        if (Enum.TryParse<ErrorHandlingMode>(configValue, true, out var configMode))
        {
            return configMode;
        }

        return ErrorHandlingMode.Basic;
    }
}