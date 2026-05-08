using App.VectorStore;

namespace App.Abstractions;

public interface IVectorStore<TPointPayload>
{
    Task UpsertAsync(IReadOnlyList<VectorPoint<TPointPayload>> points, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit<TPointPayload>>> SearchAsync(double[] queryVector, int topK, Guid? filterByDocId, CancellationToken ct = default);
}