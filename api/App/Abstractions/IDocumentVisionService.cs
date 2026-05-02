using App.DocumentVision;

namespace App.Abstractions;

public interface IDocumentVisionService
{
    Task<string> AnalyzeDocumentAsync(Stream fileStream, string fileName, AnalyzeDocumentOptions options, CancellationToken ct = default);
}