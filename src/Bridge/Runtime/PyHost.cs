using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace Bridge.Runtime;

public enum PythonDistribution
{
    Local,
    Bundled,
    Auto
}

public enum ErrorHandlingMode
{
    Basic,
    Verbose
}

public static class PyHost
{
    private static readonly object _lock = new();
    private static bool _isInitialized = false;

    public static bool IsInitialized => _isInitialized;

    public static void Initialize(string? pythonDllPath = null, IEnumerable<string>? extraSysPaths = null)
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;

            try
            {
                var distribution = GetDistributionMode();

                if (!string.IsNullOrEmpty(pythonDllPath))
                {
                    Python.Runtime.Runtime.PythonDLL = pythonDllPath;
                }
                else
                {
                    var envDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
                    if (!string.IsNullOrEmpty(envDll))
                    {
                        Python.Runtime.Runtime.PythonDLL = envDll;
                    }
                }

                PythonEngine.Initialize();

                AddSysPaths(extraSysPaths);

                AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to initialize Python runtime: {ex.Message}");
                Console.WriteLine("Continuing with C# implementations only...");
            }
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            if (!_isInitialized)
                return;

            try
            {
                PythonEngine.Shutdown();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error during Python shutdown: {ex.Message}");
            }
        }
    }

    private static PythonDistribution GetDistributionMode()
    {
        var envValue = Environment.GetEnvironmentVariable("NETPYBRIDGE_DISTRIBUTION");
        if (Enum.TryParse<PythonDistribution>(envValue, true, out var mode))
        {
            return mode;
        }
        return PythonDistribution.Auto;
    }

    private static void AddSysPaths(IEnumerable<string>? extraSysPaths)
    {
        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            dynamic path = sys.path;

            var pySrcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "src", "PyAuthored", "py_src");
            var fullPath = Path.GetFullPath(pySrcPath);
            if (Directory.Exists(fullPath))
            {
                path.insert(0, fullPath);
            }

            var envPaths = Environment.GetEnvironmentVariable("NETPYBRIDGE_PY_PATHS");
            if (!string.IsNullOrEmpty(envPaths))
            {
                var paths = envPaths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pathItem in paths)
                {
                    if (Directory.Exists(pathItem))
                    {
                        path.insert(0, pathItem);
                    }
                }
            }

            if (extraSysPaths != null)
            {
                foreach (var pathItem in extraSysPaths)
                {
                    if (Directory.Exists(pathItem))
                    {
                        path.insert(0, pathItem);
                    }
                }
            }
        }
    }
}