using Bridge.Abstractions;
using Bridge.Runtime;
using DotNetImpls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== NetPyBridge.RnD Demo ===");
        Console.WriteLine();

        // Initialize Python runtime (will gracefully fail if Python not available)
        PyHost.Initialize();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Register C# implementation
        services.AddScoped<ITextOps, TextOpsDotNet>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var textOps = scope.ServiceProvider.GetRequiredService<ITextOps>();

        Console.WriteLine("Testing C# implementation of ITextOps:");
        Console.WriteLine();

        // Test synchronous method
        var slug = textOps.Slugify("Hello World from C#!");
        Console.WriteLine($"✅ Slugify('Hello World from C#!'): {slug}");

        // Test asynchronous method
        var summary = await textOps.SummarizeAsync("This is a very long text that should be summarized by the C# implementation. It contains multiple sentences and should be truncated to demonstrate the async functionality.");
        Console.WriteLine($"✅ SummarizeAsync result: {summary}");

        Console.WriteLine();
        Console.WriteLine("🎉 Demo completed successfully!");
        Console.WriteLine();
        Console.WriteLine("Key Features Demonstrated:");
        Console.WriteLine("• Interface-based programming with dependency injection");
        Console.WriteLine("• Synchronous and asynchronous method calls");
        Console.WriteLine("• Scoped lifetime management");
        Console.WriteLine("• Graceful Python runtime initialization");
        Console.WriteLine();
        Console.WriteLine("The bridge is ready for Python implementations when Python is properly configured!");
    }
}
