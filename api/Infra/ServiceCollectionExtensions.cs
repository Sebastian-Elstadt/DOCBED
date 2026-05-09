using System.Net.Http.Headers;
using App.Abstractions;
using App.VectorStore;
using Infra.Configs;
using Infra.DocumentConverter;
using Infra.DocumentVision;
using Infra.Embedding;
using Infra.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
    {
        var togetherAiConfig = config.GetRequiredSection("TogetherAI").Get<TogetherAIConfig>();
        if (togetherAiConfig is null) throw new InvalidOperationException("TogetherAI config is required.");
        togetherAiConfig.EnsureValid();

        services.AddHttpClient<IDocumentConverterService, PythonDocumentConverterService>(client =>
        {
            client.BaseAddress = new Uri("http://doc-converter:8000/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient<IDocumentVisionService, TogetherAiDocumentVisionService>(client =>
        {
            client.BaseAddress = new Uri(togetherAiConfig.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", togetherAiConfig.ApiKey);
        });

        services.AddHttpClient<IEmbeddingService, TogetherAiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(togetherAiConfig.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", togetherAiConfig.ApiKey);
        });

        services.AddSingleton(sp =>
        {
            var qdrantConfig = config.GetRequiredSection("Qdrant").Get<QdrantConfig>();
            if (qdrantConfig is null) throw new InvalidOperationException("Qdrant config is required.");
            qdrantConfig.EnsureValid();

            return new QdrantClient(qdrantConfig.Host, qdrantConfig.Port, qdrantConfig.Https);
        });
        services.AddSingleton<IVectorStore<DocumentPageVectorData>, QdrantDocumentPagesVectorStore>();

        return services;
    }
}
