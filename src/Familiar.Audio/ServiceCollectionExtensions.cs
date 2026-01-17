using Microsoft.Extensions.DependencyInjection;

namespace Familiar.Audio;

/// <summary>
/// Extension methods for registering audio services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Familiar audio services to the service collection.
    /// </summary>
    public static IServiceCollection AddFamiliarAudio(this IServiceCollection services)
    {
        services.AddSingleton<IAudioPlayer, AlsaAudioPlayer>();
        services.AddSingleton<IAudioCapture, AlsaAudioCapture>();
        services.AddSingleton<IAudioManager, AudioManager>();

        return services;
    }
}
