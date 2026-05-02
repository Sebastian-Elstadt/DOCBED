using System.Text.Json.Serialization;

namespace App.DocumentVision;

public record PageAnalysisResult(
    [property: JsonPropertyName("page_number")]
    int PageNumber,
    [property: JsonPropertyName("content_summary")]
    string ContentSummary,
    [property: JsonPropertyName("extracted_text")]
    string ExtractedText,
    [property: JsonPropertyName("tables")]
    List<object> Tables,
    [property: JsonPropertyName("charts_diagrams")]
    List<object> ChartsDiagrams,
    [property: JsonPropertyName("key_entities")]
    List<string> KeyEntities,
    [property: JsonPropertyName("layout_type")]
    string LayoutType
);