using App.Abstractions;
using Infra.DocumentVision;
using Microsoft.Extensions.DependencyInjection;

namespace Infra;

public static class ServiceCollectionExtensions
{
    private static IServiceCollection AddInfra(this IServiceCollection services)
    {
        services.AddHttpClient<IDocumentVisionService, TogetherAiQwenDocumentVisionService>(client =>
        {
            client.BaseAddress = new Uri("https://api.together.ai/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        return services;
    }
}
