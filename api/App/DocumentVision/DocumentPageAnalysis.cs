using System.Text.Json.Serialization;

namespace App.DocumentVision;

public record DocumentPageAnalysis
{
    public byte[] PageHash { get; init; } = [];

    [JsonPropertyName("page_number")]
    public int PageNumber { get; init; }

    [JsonPropertyName("content_summary")]
    public string ContentSummary { get; init; } = string.Empty;

    [JsonPropertyName("extracted_text")]
    public string ExtractedText { get; init; } = string.Empty;

    [JsonPropertyName("tables")]
    public List<object> Tables { get; init; } = [];

    [JsonPropertyName("charts_diagrams")]
    public List<ChartDiagram> ChartsDiagrams { get; init; } = [];

    [JsonPropertyName("key_entities")]
    public List<string> KeyEntities { get; init; } = [];

    [JsonPropertyName("layout_type")]
    public string LayoutType { get; init; } = string.Empty;

    public string GenerateEmbeddingText()
    {
        return string.Join("\n\n", new[]
        {
            $"Page {PageNumber}: {ContentSummary}",
            ExtractedText,
            string.Join("\n", ChartsDiagrams.Select(cd => cd.Description).Where(s => !string.IsNullOrWhiteSpace(s)))
        });
    }

    public record ChartDiagram(
        [property: JsonPropertyName("type")]
        string Type,
        [property: JsonPropertyName("description")]
        string Description,
        [property: JsonPropertyName("data")]
        string Data
    );
}