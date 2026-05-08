namespace App.Abstractions;

public interface IEmbeddingService
{
    Task<IEnumerable<double[]>> GenerateEmbeddingsAsync(string[] inputs, CancellationToken ct = default);
}