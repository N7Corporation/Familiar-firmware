using Microsoft.Extensions.DependencyInjection;

namespace Familiar.Tts;

/// <summary>
/// Extension methods for registering TTS services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Familiar TTS services to the service collection.
    /// </summary>
    public static IServiceCollection AddFamiliarTts(this IServiceCollection services)
    {
        services.AddSingleton<ITtsEngine, EspeakTtsEngine>();
        return services;
    }
}
