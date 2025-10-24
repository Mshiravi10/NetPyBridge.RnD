using System.Reflection;
using Bridge.Abstractions;
using Bridge.Runner.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bridge.Runner;

public class OperationExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OperationExecutor> _logger;
    private readonly Dictionary<string, Type> _interfaceRegistry;
    private readonly Dictionary<string, MethodInfo> _methodCache;

    public OperationExecutor(IServiceProvider serviceProvider, ILogger<OperationExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interfaceRegistry = new Dictionary<string, Type>();
        _methodCache = new Dictionary<string, MethodInfo>();

        RegisterInterfaces();
    }

    public async Task<object?> ExecuteAsync(OperationCall operation)
    {
        using var scope = _serviceProvider.CreateScope();

        if (!_interfaceRegistry.TryGetValue(operation.Interface, out var interfaceType))
        {
            throw new InvalidOperationException($"Interface '{operation.Interface}' not registered");
        }

        var service = scope.ServiceProvider.GetRequiredService(interfaceType);
        var methodKey = $"{operation.Interface}.{operation.Method}";

        if (!_methodCache.TryGetValue(methodKey, out var method))
        {
            method = interfaceType.GetMethod(operation.Method);
            if (method == null)
            {
                throw new InvalidOperationException($"Method '{operation.Method}' not found on interface '{operation.Interface}'");
            }
            _methodCache[methodKey] = method;
        }

        var args = MapArguments(method, operation.Args);

        _logger.LogInformation("Executing {Interface}.{Method} with {ArgCount} arguments",
            operation.Interface, operation.Method, args.Length);

        var result = method.Invoke(service, args);

        if (result is Task task)
        {
            await task;

            if (task.GetType().IsGenericType)
            {
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            return null;
        }

        return result;
    }

    private void RegisterInterfaces()
    {
        _interfaceRegistry["ITextOps"] = typeof(ITextOps);
    }

    private object?[] MapArguments(MethodInfo method, Dictionary<string, object> args)
    {
        var parameters = method.GetParameters();
        var mappedArgs = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name ?? $"arg{i}";

            if (args.TryGetValue(paramName, out var argValue))
            {
                mappedArgs[i] = ConvertValue(argValue, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                mappedArgs[i] = param.DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Required parameter '{paramName}' not provided");
            }
        }

        return mappedArgs;
    }

    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        if (targetType == typeof(CancellationToken))
            return CancellationToken.None;

        return Convert.ChangeType(value, targetType);
    }
}
