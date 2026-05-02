using System.Net.Http.Headers;
using App.Abstractions;
using Infra.Configs;
using Infra.DocumentConverter;
using Infra.DocumentVision;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IDocumentConverterService, PythonDocumentConverterService>(client =>
        {
            client.BaseAddress = new Uri("http://doc-converter:8000/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient<IDocumentVisionService, TogetherAiQwenDocumentVisionService>(client =>
        {
            var cfg = config.GetRequiredSection("TogetherAI").Get<TogetherAIConfig>();
            if (cfg is null) throw new InvalidOperationException("TogetherAI config is required.");
            cfg.EnsureValid();

            client.BaseAddress = new Uri("https://api.together.ai/");
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        });

        return services;
    }
}
