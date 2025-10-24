using System.Text.Json.Serialization;

namespace Bridge.Abstractions.Dto;

public class SummaryResult
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("originalLength")]
    public int OriginalLength { get; set; }

    [JsonPropertyName("summaryLength")]
    public int SummaryLength { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
