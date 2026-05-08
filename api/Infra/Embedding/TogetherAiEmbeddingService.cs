using System.Net.Http.Json;
using System.Text.Json;
using App.Abstractions;
using Microsoft.Extensions.Logging;

namespace Infra.Embedding;

public class TogetherAiEmbeddingService(
    HttpClient httpClient,
    ILogger<TogetherAiEmbeddingService> logger
) : IEmbeddingService
{
    private record EmbeddingModelResponse(
        List<EmbeddingModelResponse.DataEntry> data
    )
    {
        public record DataEntry(
            double[] embedding
        );
    }

    public async Task<IEnumerable<double[]>> GenerateEmbeddingsAsync(string[] inputs, CancellationToken ct = default)
    {
        logger.LogInformation($"Generating embeddings for {inputs.Count()} inputs...");
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings")
        {
            Content = JsonContent.Create(new
            {
                model = "intfloat/multilingual-e5-large-instruct",
                input = inputs
            })
        };

        using var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync(ct);
            throw new Exception("TogetherAI Request failed: " + res);
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingModelResponse>(ct);
        if (result is null) throw new JsonException("TogetherAI embedding model response could not be parsed.");
        return result.data.Select(x => x.embedding);
    }
}