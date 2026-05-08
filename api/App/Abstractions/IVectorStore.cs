using App.VectorStore;

namespace App.Abstractions;

public interface IVectorStore
{
    Task UpsertAsync<T>(IReadOnlyList<VectorPoint<T>> points, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit<T>>> SearchAsync<T>(double[] queryVector, int topK, Guid? filterByDocId, CancellationToken ct = default);
}