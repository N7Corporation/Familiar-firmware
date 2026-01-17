using Microsoft.Extensions.DependencyInjection;

namespace Familiar.Meshtastic;

/// <summary>
/// Extension methods for registering Meshtastic services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Familiar Meshtastic services to the service collection.
    /// </summary>
    public static IServiceCollection AddFamiliarMeshtastic(this IServiceCollection services)
    {
        services.AddSingleton<IMeshtasticClient, MeshtasticClient>();
        services.AddSingleton<MeshtasticService>();
        services.AddHostedService(sp => sp.GetRequiredService<MeshtasticService>());

        return services;
    }
}
