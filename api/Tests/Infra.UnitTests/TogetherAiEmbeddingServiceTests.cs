using System.Net;
using System.Text.Json;
using Infra.Embedding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infra.UnitTests;

public class TogetherAiEmbeddingServiceTests
{
    private static (TogetherAiEmbeddingService svc, StubHttpMessageHandler handler) Build(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        var handler = new StubHttpMessageHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.together.xyz/") };
        var svc = new TogetherAiEmbeddingService(http, NullLogger<TogetherAiEmbeddingService>.Instance);
        return (svc, handler);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_PostsToEmbeddingsEndpointWithModelAndInputs()
    {
        var responseBody = """{"data":[{"embedding":[0.1,0.2]},{"embedding":[0.3,0.4]}]}""";
        var (svc, handler) = Build(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        }));

        var result = await svc.GenerateEmbeddingsAsync(new[] { "passage: foo", "passage: bar" });

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/v1/embeddings", handler.Requests[0].RequestUri!.AbsolutePath);

        using var doc = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("intfloat/multilingual-e5-large-instruct", doc.RootElement.GetProperty("model").GetString());
        var inputs = doc.RootElement.GetProperty("input");
        Assert.Equal(2, inputs.GetArrayLength());
        Assert.Equal("passage: foo", inputs[0].GetString());
        Assert.Equal("passage: bar", inputs[1].GetString());

        var vectors = result.ToArray();
        Assert.Equal(2, vectors.Length);
        Assert.Equal(new[] { 0.1, 0.2 }, vectors[0]);
        Assert.Equal(new[] { 0.3, 0.4 }, vectors[1]);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ThrowsWithBodyOnNonSuccess()
    {
        var (svc, _) = Build(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"nope"}}""")
        }));

        var ex = await Assert.ThrowsAsync<Exception>(() => svc.GenerateEmbeddingsAsync(new[] { "x" }));
        Assert.Contains("nope", ex.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ThrowsOnEmptyResponseBody()
    {
        var (svc, _) = Build(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        }));

        await Assert.ThrowsAsync<JsonException>(() => svc.GenerateEmbeddingsAsync(new[] { "x" }));
    }
}
