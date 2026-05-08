using System.Net.Http.Headers;
using App.Abstractions;
using Infra.Configs;
using Infra.DocumentConverter;
using Infra.DocumentVision;
using Infra.Embedding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
