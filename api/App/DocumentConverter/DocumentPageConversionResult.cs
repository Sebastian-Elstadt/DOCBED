namespace App.DocumentConverter;

public record DocumentPageConversionResult(
    bool Success,
    int Page,
    string? ImageBase64,
    string? Format,
    string? Error = null
);
