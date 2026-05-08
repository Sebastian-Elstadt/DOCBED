using App.DocumentVision;

namespace App.Abstractions;

public interface IDocumentVisionService
{
    DocumentAnalysis AnalyzeDocumentAsync(Stream fileStream, string fileName, AnalyzeDocumentOptions options);
}