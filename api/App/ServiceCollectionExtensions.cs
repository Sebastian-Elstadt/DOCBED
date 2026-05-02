using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services)
    {
        return services;
    }
}
