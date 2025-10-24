using System.Text.Json;
using Bridge.Abstractions;
using Bridge.Runtime;
using Bridge.Runner;
using Bridge.Runner.Models;
using DotNetImpls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleRunner;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            PyHost.Initialize();
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            var serviceProvider = services.BuildServiceProvider();
            
            if (args.Length > 0 && args[0] == "manifest")
            {
                await RunWithManifest(serviceProvider);
            }
            else
            {
                await RunInteractive(serviceProvider);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
        services.AddScoped<OperationExecutor>();
    }

    private static async Task RunWithManifest(IServiceProvider serviceProvider)
    {
        var manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Manifest", "ops.manifest.json");
        
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"Manifest file not found: {manifestPath}");
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<ManifestEntry[]>(manifestJson);
        
        if (manifest == null)
        {
            Console.WriteLine("Failed to parse manifest");
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        
        foreach (var entry in manifest)
        {
            RegisterFromManifest(services, entry);
        }
        
        var scopedProvider = services.BuildServiceProvider();
        
        Console.WriteLine("Running operations from manifest...");
        
        var executor = scopedProvider.GetRequiredService<OperationExecutor>();
        
        var operations = new[]
        {
            new OperationCall
            {
                Interface = "ITextOps",
                Method = "Slugify",
                Args = new Dictionary<string, object> { ["input"] = "Hello World Test" },
                ResultKey = "slug"
            },
            new OperationCall
            {
                Interface = "ITextOps",
                Method = "SummarizeAsync",
                Args = new Dictionary<string, object> { ["text"] = "This is a very long text that should be summarized because it exceeds the maximum length limit" },
                ResultKey = "summary"
            }
        };
        
        foreach (var operation in operations)
        {
            try
            {
                var result = await executor.ExecuteAsync(operation);
                Console.WriteLine($"{operation.Interface}.{operation.Method} -> {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing {operation.Interface}.{operation.Method}: {ex.Message}");
            }
        }
    }

    private static void RegisterFromManifest(IServiceCollection services, ManifestEntry entry)
    {
        switch (entry.Interface)
        {
            case "ITextOps":
                if (entry.Impl == "python")
                {
                    services.AddPythonBridge<ITextOps>(
                        entry.Module,
                        entry.Class,
                        provider => entry.CtorArgs?.PassEnv == true ? new object[] { new { TenantId = "test" } } : Array.Empty<object>(),
                        ParseLifetime(entry.Lifetime));
                }
                else
                {
                    services.AddScoped<ITextOps, TextOpsDotNet>();
                }
                break;
        }
    }

    private static ServiceLifetime ParseLifetime(string lifetime)
    {
        return lifetime?.ToLower() switch
        {
            "singleton" => ServiceLifetime.Singleton,
            "transient" => ServiceLifetime.Transient,
            _ => ServiceLifetime.Scoped
        };
    }

    private static async Task RunInteractive(IServiceProvider serviceProvider)
    {
        Console.WriteLine("NetPyBridge Console Runner");
        Console.WriteLine("Commands:");
        Console.WriteLine("  1. Test Python implementation");
        Console.WriteLine("  2. Test C# implementation");
        Console.WriteLine("  3. Test dynamic operation");
        Console.WriteLine("  q. Quit");
        
        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            
            switch (input?.ToLower())
            {
                case "1":
                    await TestPythonImplementation(serviceProvider);
                    break;
                case "2":
                    await TestDotNetImplementation(serviceProvider);
                    break;
                case "3":
                    await TestDynamicOperation(serviceProvider);
                    break;
                case "q":
                    return;
                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
        }
    }

    private static async Task TestPythonImplementation(IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");
        
        var scopedProvider = services.BuildServiceProvider();
        var textOps = scopedProvider.GetRequiredService<ITextOps>();
        
        Console.WriteLine("Testing Python implementation:");
        Console.WriteLine($"Slugify: {textOps.Slugify("Hello World Test")}");
        Console.WriteLine($"SummarizeAsync: {await textOps.SummarizeAsync("This is a very long text that should be summarized")}");
    }

    private static async Task TestDotNetImplementation(IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        services.AddScoped<ITextOps, TextOpsDotNet>();
        
        var scopedProvider = services.BuildServiceProvider();
        var textOps = scopedProvider.GetRequiredService<ITextOps>();
        
        Console.WriteLine("Testing C# implementation:");
        Console.WriteLine($"Slugify: {textOps.Slugify("Hello World Test")}");
        Console.WriteLine($"SummarizeAsync: {await textOps.SummarizeAsync("This is a very long text that should be summarized")}");
    }

    private static async Task TestDynamicOperation(IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");
        
        var scopedProvider = services.BuildServiceProvider();
        var executor = scopedProvider.GetRequiredService<OperationExecutor>();
        
        var operation = new OperationCall
        {
            Interface = "ITextOps",
            Method = "Slugify",
            Args = new Dictionary<string, object> { ["input"] = "Dynamic Test Input" },
            ResultKey = "result"
        };
        
        var result = await executor.ExecuteAsync(operation);
        Console.WriteLine($"Dynamic operation result: {result}");
    }
}

public class ManifestEntry
{
    public string Interface { get; set; } = string.Empty;
    public string Impl { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Lifetime { get; set; } = "Scoped";
    public CtorArgs? CtorArgs { get; set; }
}

public class CtorArgs
{
    public bool PassEnv { get; set; }
}
