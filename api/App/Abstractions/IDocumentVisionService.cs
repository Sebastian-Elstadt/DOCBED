using App.DocumentVision;

namespace App.Abstractions;

public interface IDocumentVisionService
{
    DocumentAnalysis AnalyzeDocument(Stream fileStream, string fileName, AnalyzeDocumentOptions options);
}