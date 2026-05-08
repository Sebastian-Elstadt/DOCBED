namespace App.VectorStore;

public sealed record VectorSearchHit<T>(
    Guid Id,
    float Score,
    T Data
);