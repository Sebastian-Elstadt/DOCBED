namespace App.DocumentVision;

public record AnalyzeDocumentOptions(
    double Temperature,
    uint MaxTokens
);