# NetPyBridge.RnD

A production-grade research and development project demonstrating a method-to-method bridge between .NET 8 (C# 12) and CPython using pythonnet. This bridge allows interfaces defined in .NET to be implemented in either C# or Python, with seamless integration into Microsoft.Extensions.DependencyInjection.

## Overview

NetPyBridge.RnD validates design patterns, ergonomics, testing, and deployment strategies for bridging .NET and Python ecosystems. The bridge supports:

- **Interface Contracts**: Define contracts in .NET, implement in C# or Python
- **Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection (Scoped/Transient/Singleton)
- **Type Mapping**: Automatic conversion between C# and Python types
- **Async Support**: Handle both sync and async Python methods
- **Dynamic Invocation**: Execute operations from JSON definitions
- **Manifest-Driven**: Configure implementations via JSON manifests

## Quick Start

### Prerequisites

- .NET 8 SDK
- Python 3.12 (or use bundled Python)

### Setup Options

#### Option 1: Local Python Installation

1. Install Python 3.12
2. Set environment variable:
   ```bash
   # Windows
   set PYTHONNET_PYDLL=python312.dll
   
   # Linux
   export PYTHONNET_PYDLL=/usr/lib/x86_64-linux-gnu/libpython3.12.so
   ```

#### Option 2: Bundled Python (Python.Included)

1. Set environment variable:
   ```bash
   set NETPYBRIDGE_DISTRIBUTION=Bundled
   ```

#### Option 3: Auto Detection

The bridge will automatically detect available Python installations.

### Running the Demo

#### Console Application

```bash
# Run interactive mode
dotnet run --project src/Bridge/Hosting/ConsoleRunner

# Run with manifest
dotnet run --project src/Bridge/Hosting/ConsoleRunner -- manifest
```

#### Web API

```bash
dotnet run --project src/Bridge/Hosting/DemoApi
```

Then test the endpoints:

```bash
# Slugify text
curl "http://localhost:5000/slug?value=Hello%20World%20Test"

# Summarize text
curl -X POST "http://localhost:5000/summarize" -H "Content-Type: application/json" -d '"This is a very long text that should be summarized"'

# Dynamic operation
curl -X POST "http://localhost:5000/op/run" -H "Content-Type: application/json" -d '{
  "interface": "ITextOps",
  "method": "Slugify",
  "args": { "input": "Hello Bridge" },
  "resultKey": "slug"
}'
```

## Configuration

### Environment Variables

- `PYTHONNET_PYDLL`: Path to Python DLL (e.g., `python312.dll` or `libpython3.12.so`)
- `NETPYBRIDGE_PY_PATHS`: Comma/semicolon-separated additional Python paths
- `NETPYBRIDGE_ERROR_MODE`: Error handling mode (`Basic` or `Verbose`)
- `NETPYBRIDGE_DISTRIBUTION`: Python distribution (`Local`, `Bundled`, or `Auto`)

### AppSettings Configuration

```json
{
  "ErrorHandling": {
    "Mode": "Basic"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Usage Examples

### Defining an Interface

```csharp
public interface ITextOps
{
    string Slugify(string input);
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);
}
```

### Python Implementation

```python
class TextOps:
    def __init__(self, env=None):
        self.env = env
    
    def Slugify(self, input: str) -> str:
        return input.lower().strip().replace(" ", "-")
    
    async def SummarizeAsync(self, text: str) -> str:
        return (text[:120] + "...") if len(text) > 120 else text
```

### C# Implementation

```csharp
public class TextOpsDotNet : ITextOps
{
    public string Slugify(string input)
    {
        return input.ToLower().Trim().Replace(" ", "-");
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return text.Length > 120 ? text[..120] + "..." : text;
    }
}
```

### Dependency Injection Registration

```csharp
// Python implementation
services.AddPythonBridge<ITextOps>("netpy_ops.text", "TextOps");

// C# implementation
services.AddScoped<ITextOps, TextOpsDotNet>();
```

### Manifest-Driven Registration

```json
[
  {
    "interface": "ITextOps",
    "impl": "python",
    "module": "netpy_ops.text",
    "class": "TextOps",
    "lifetime": "Scoped",
    "ctorArgs": { "passEnv": true }
  }
]
```

## Generating Python Stubs

Use the StubGen tool to generate Python class skeletons from C# interfaces:

```bash
dotnet run --project src/Tools/StubGen -- --interface Bridge.Abstractions.ITextOps --out py_src/netpy_ops/text.py
```

## Type Mapping

### Primitive Types

| C# Type | Python Type |
|---------|-------------|
| `string` | `str` |
| `int` | `int` |
| `long` | `int` |
| `double` | `float` |
| `float` | `float` |
| `bool` | `bool` |

### Complex Types

Complex objects are automatically serialized to JSON and passed between C# and Python. Use the `[BridgeJson]` attribute to force JSON serialization:

```csharp
[BridgeJson]
public class SummaryRequest
{
    public string Text { get; set; }
    public int MaxLength { get; set; }
}
```

## Async Handling

The bridge supports both synchronous and asynchronous Python methods:

- **Sync Methods**: Called directly and return results immediately
- **Async Methods**: Detected via `inspect.iscoroutinefunction()` and executed with `asyncio.run()`
- **Task Return Types**: Python coroutines are wrapped in `Task<T>` for C# async/await

## Error Handling

### Basic Mode (Default)

- Captures exception type and message
- Minimal overhead

### Verbose Mode

- Full Python stack traces
- Invocation context (interface, method, arguments)
- Optional locals capture (disabled by default for security)

Enable verbose mode:

```bash
set NETPYBRIDGE_ERROR_MODE=Verbose
```

## Security Considerations

### Module Allow-List

Restrict Python module imports to trusted sources:

```csharp
// Only allow modules from specific paths
PyHost.Initialize(extraSysPaths: new[] { "/trusted/python/modules" });
```

### Sandbox Limitations

- Python code runs in the same process as .NET
- No automatic sandboxing or isolation
- Consider using separate processes for untrusted code

## Known Limitations

### GIL (Global Interpreter Lock)

- Python operations are serialized due to GIL
- CPU-bound Python workloads may impact .NET performance
- Consider using multiprocessing for CPU-intensive tasks

### Long-Running Operations

- Python operations block the calling thread
- Use async patterns and cancellation tokens where possible
- Consider timeout mechanisms for long-running calls

### Memory Management

- Python objects are managed by CPython's garbage collector
- Ensure proper disposal of Python proxies
- Monitor memory usage in long-running applications

## Testing

Run the test suite:

```bash
dotnet test
```

Run with verbose error mode:

```bash
set NETPYBRIDGE_ERROR_MODE=Verbose
dotnet test
```

## Building

```bash
dotnet build
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] Event loop integration for true async Python support
- [ ] Additional type converters (DateTime, Guid, etc.)
- [ ] Performance profiling and optimization
- [ ] Docker containerization examples
- [ ] Kubernetes deployment manifests
- [ ] Monitoring and observability integration
