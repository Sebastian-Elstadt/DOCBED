using App.DocumentConverter;

namespace App.Abstractions;

public interface IDocumentConverterService
{
    IAsyncEnumerable<DocumentPage> ConvertToPageImagesAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}