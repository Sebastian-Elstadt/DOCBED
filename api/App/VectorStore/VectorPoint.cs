namespace App.VectorStore;

public sealed record VectorPoint<T>(
    Guid Id,
    double[] Vector,
    T Data
);