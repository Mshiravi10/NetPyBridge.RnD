using System.Reflection;
using Bridge.Abstractions;
using Bridge.Runtime.Converters;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace Bridge.Runtime;

public class PythonBridgeException : Exception
{
    public string PythonType { get; }
    public string PythonMessage { get; }
    public string? PythonStack { get; }
    public InvocationInfo? InvocationInfo { get; }
    public Dictionary<string, object>? Locals { get; }

    public PythonBridgeException(
        string pythonType,
        string pythonMessage,
        string? pythonStack = null,
        InvocationInfo? invocationInfo = null,
        Dictionary<string, object>? locals = null,
        Exception? innerException = null)
        : base($"Python {pythonType}: {pythonMessage}", innerException)
    {
        PythonType = pythonType;
        PythonMessage = pythonMessage;
        PythonStack = pythonStack;
        InvocationInfo = invocationInfo;
        Locals = locals;
    }
}

public record InvocationInfo(string InterfaceName, string MethodName, object[] Arguments);

public interface IPythonAsyncStrategy
{
    Task<T> ExecuteAsync<T>(PyObject coroutine, CancellationToken cancellationToken = default);
}

public class SimpleAsyncStrategy : IPythonAsyncStrategy
{
    public async Task<T> ExecuteAsync<T>(PyObject coroutine, CancellationToken cancellationToken = default)
    {
        using (Py.GIL())
        {
            dynamic asyncio = Py.Import("asyncio");
            dynamic result = asyncio.run(coroutine);

            return await Task.FromResult(PrimitiveConverter.FromPython<T>(result));
        }
    }
}

public class PythonProxy<T> : DispatchProxy where T : class
{
    private PyObject? _target;
    private ILogger<PythonProxy<T>>? _logger;
    private IPythonAsyncStrategy? _asyncStrategy;
    private ErrorHandlingMode _errorMode;

    public void Initialize(PyObject target, ILogger<PythonProxy<T>> logger, IPythonAsyncStrategy asyncStrategy, ErrorHandlingMode errorMode = ErrorHandlingMode.Basic)
    {
        _target = target;
        _logger = logger;
        _asyncStrategy = asyncStrategy;
        _errorMode = errorMode;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (_target == null || targetMethod == null || _logger == null || _asyncStrategy == null)
            throw new InvalidOperationException("Proxy not properly initialized");

        try
        {
            using (Py.GIL())
            {
                var methodName = targetMethod.Name;
                dynamic pyMethod = _target.GetAttr(methodName);

                if (pyMethod == null)
                    throw new InvalidOperationException($"Method '{methodName}' not found on Python object");

                var isCoroutine = IsCoroutine(pyMethod);
                var returnType = targetMethod.ReturnType;

                if (isCoroutine && IsTaskType(returnType))
                {
                    return ExecuteAsyncMethod(pyMethod, args, returnType);
                }
                else if (isCoroutine)
                {
                    var result = ExecuteCoroutineSync(pyMethod, args);
                    return ConvertResult(result, returnType);
                }
                else
                {
                    var result = ExecuteSyncMethod(pyMethod, args);
                    return ConvertResult(result, returnType);
                }
            }
        }
        catch (Exception ex) when (!(ex is PythonBridgeException))
        {
            throw CreatePythonException(ex, targetMethod, args);
        }
    }

    private bool IsCoroutine(dynamic pyMethod)
    {
        try
        {
            using (Py.GIL())
            {
                dynamic inspect = Py.Import("inspect");
                return (bool)inspect.iscoroutinefunction(pyMethod);
            }
        }
        catch
        {
            return false;
        }
    }

    private bool IsTaskType(Type returnType)
    {
        return returnType == typeof(Task) ||
               (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    private object ExecuteAsyncMethod(dynamic pyMethod, object?[]? args, Type returnType)
    {
        var pyArgs = ConvertArguments(args);
        dynamic coroutine = pyArgs.Length == 0 ? pyMethod() : pyMethod(pyArgs);

        if (returnType == typeof(Task))
        {
            return _asyncStrategy!.ExecuteAsync<object>(coroutine);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var executeMethod = typeof(IPythonAsyncStrategy).GetMethod(nameof(IPythonAsyncStrategy.ExecuteAsync))!
                .MakeGenericMethod(resultType);
            return executeMethod.Invoke(_asyncStrategy, new object[] { coroutine, CancellationToken.None })!;
        }

        throw new InvalidOperationException($"Unsupported async return type: {returnType}");
    }

    private object ExecuteCoroutineSync(dynamic pyMethod, object?[]? args)
    {
        var pyArgs = ConvertArguments(args);
        dynamic coroutine = pyArgs.Length == 0 ? pyMethod() : pyMethod(pyArgs);

        dynamic asyncio = Py.Import("asyncio");
        return asyncio.run(coroutine);
    }

    private object ExecuteSyncMethod(dynamic pyMethod, object?[]? args)
    {
        var pyArgs = ConvertArguments(args);
        return pyArgs.Length == 0 ? pyMethod() : pyMethod(pyArgs);
    }

    private PyObject[] ConvertArguments(object?[]? args)
    {
        if (args == null || args.Length == 0)
            return Array.Empty<PyObject>();

        var pyArgs = new PyObject[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            pyArgs[i] = PrimitiveConverter.ToPython(args[i]);
        }
        return pyArgs;
    }

    private object? ConvertResult(object result, Type targetType)
    {
        if (targetType == typeof(void) || targetType == typeof(Task))
            return null;

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = targetType.GetGenericArguments()[0];
            var pyResult = result as PyObject ?? ((dynamic)result).ToPython();
            var convertedResult = PrimitiveConverter.FromPython(pyResult, resultType);
            return Task.FromResult(convertedResult);
        }

        var pyObj = result as PyObject ?? ((dynamic)result).ToPython();
        return PrimitiveConverter.FromPython(pyObj, targetType);
    }

    private PythonBridgeException CreatePythonException(Exception ex, MethodInfo method, object?[]? args)
    {
        var interfaceName = typeof(T).Name;
        var methodName = method.Name;
        var invocationInfo = new InvocationInfo(interfaceName, methodName, args ?? Array.Empty<object>());

        if (_errorMode == ErrorHandlingMode.Verbose)
        {
            return new PythonBridgeException(
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace,
                invocationInfo,
                null,
                ex);
        }
        else
        {
            return new PythonBridgeException(
                ex.GetType().Name,
                ex.Message,
                invocationInfo: invocationInfo,
                innerException: ex);
        }
    }

    public void Dispose()
    {
        _target?.Dispose();
        _target = null;
    }
}