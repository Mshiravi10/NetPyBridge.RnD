using System.Text.Json.Serialization;

namespace Bridge.Abstractions;

public interface ITextOps
{
    string Slugify(string input);
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);
}
