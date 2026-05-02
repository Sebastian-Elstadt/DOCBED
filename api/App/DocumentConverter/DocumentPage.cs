namespace App.DocumentConverter;

public record DocumentPage(
    bool Success,
    int Page,
    string? ImageBase64,
    string? Format,
    string? Error = null
);
