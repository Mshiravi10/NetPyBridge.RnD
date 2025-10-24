using System.Text.Json.Serialization;

namespace Bridge.Abstractions.Dto;

public class SummaryRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("maxLength")]
    public int MaxLength { get; set; } = 120;

    [JsonPropertyName("includeMetadata")]
    public bool IncludeMetadata { get; set; } = false;
}
