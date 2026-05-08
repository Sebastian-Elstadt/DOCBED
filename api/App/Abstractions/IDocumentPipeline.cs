using App.VectorStore;

namespace App.Abstractions;

public interface IDocumentPipeline
{
    Task IngestAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit<DocumentPageVectorData>>> SearchAsync(string query, int topK = 5, Guid? filterByDocId = null, CancellationToken ct = default);
}