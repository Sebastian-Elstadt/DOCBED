using App.DocumentConverter;

namespace App.Abstractions;

public interface IDocumentConverterService
{
    IAsyncEnumerable<DocumentPageConversionResult> ConvertToPageImagesAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}