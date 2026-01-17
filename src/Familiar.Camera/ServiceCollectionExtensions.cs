using Microsoft.Extensions.DependencyInjection;

namespace Familiar.Camera;

/// <summary>
/// Extension methods for registering camera services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Familiar camera services to the service collection (Pi 5 only).
    /// </summary>
    public static IServiceCollection AddFamiliarCamera(this IServiceCollection services)
    {
        services.AddSingleton<ICameraService, CameraService>();
        return services;
    }
}
