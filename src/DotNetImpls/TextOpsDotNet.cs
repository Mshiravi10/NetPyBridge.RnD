using Bridge.Abstractions;

namespace DotNetImpls;

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
