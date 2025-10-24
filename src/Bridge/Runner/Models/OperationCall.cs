using System.Text.Json.Serialization;

namespace Bridge.Runner.Models;

public class OperationCall
{
    [JsonPropertyName("interface")]
    public string Interface { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();

    [JsonPropertyName("resultKey")]
    public string ResultKey { get; set; } = string.Empty;
}
