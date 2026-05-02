using App.DocumentVision;

namespace App.Abstractions;

public interface IDocumentVisionService
{
    IAsyncEnumerable<PageAnalysisResult> AnalyzeDocumentAsync(Stream fileStream, string fileName, AnalyzeDocumentOptions options, CancellationToken ct = default);
}