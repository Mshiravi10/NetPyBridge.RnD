using System.Text.Json;
using Bridge.Abstractions;
using Bridge.Abstractions.Dto;
using Bridge.Runtime;
using Bridge.Runner;
using Bridge.Runner.Models;
using DotNetImpls;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

PyHost.Initialize();

builder.Services.AddLogging();
builder.Services.AddSingleton<IPythonAsyncStrategy, SimpleAsyncStrategy>();
builder.Services.AddScoped<OperationExecutor>();

// Always register C# implementation as fallback
builder.Services.AddScoped<ITextOps, TextOpsDotNet>();

var manifestPath = Path.Combine(builder.Environment.ContentRootPath, "Manifest", "ops.manifest.json");
if (File.Exists(manifestPath))
{
    var manifestJson = await File.ReadAllTextAsync(manifestPath);
    var manifest = JsonSerializer.Deserialize<ManifestEntry[]>(manifestJson);

    if (manifest != null)
    {
        foreach (var entry in manifest)
        {
            RegisterFromManifest(builder.Services, entry);
        }
    }
}

var app = builder.Build();

app.MapGet("/slug", async ([FromServices] ITextOps textOps, string value) =>
{
    return textOps.Slugify(value);
});

app.MapPost("/summarize", async ([FromServices] ITextOps textOps, [FromBody] string text) =>
{
    return await textOps.SummarizeAsync(text);
});

app.MapPost("/summarize-json", async ([FromServices] ITextOps textOps, [FromBody] SummaryRequest request) =>
{
    return await textOps.SummarizeAsync(request.Text);
});

app.MapPost("/op/run", async ([FromServices] OperationExecutor executor, [FromBody] OperationCall operation) =>
{
    try
    {
        var result = await executor.ExecuteAsync(operation);
        return Results.Ok(new { result, success = true });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message, success = false });
    }
});

app.Run();

static void RegisterFromManifest(IServiceCollection services, ManifestEntry entry)
{
    switch (entry.Interface)
    {
        case "ITextOps":
            if (entry.Impl == "python")
            {
                services.AddPythonBridge<ITextOps>(
                    entry.Module,
                    entry.Class,
                    provider => entry.CtorArgs?.PassEnv == true ? new object[] { new { TenantId = "api" } } : Array.Empty<object>(),
                    ParseLifetime(entry.Lifetime));
            }
            else
            {
                services.AddScoped<ITextOps, TextOpsDotNet>();
            }
            break;
    }
}

static ServiceLifetime ParseLifetime(string lifetime)
{
    return lifetime?.ToLower() switch
    {
        "singleton" => ServiceLifetime.Singleton,
        "transient" => ServiceLifetime.Transient,
        _ => ServiceLifetime.Scoped
    };
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
