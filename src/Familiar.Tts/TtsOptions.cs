namespace Familiar.Tts;

/// <summary>
/// Configuration options for text-to-speech.
/// </summary>
public record TtsOptions
{
    public const string SectionName = "Familiar:Tts";

    /// <summary>
    /// TTS engine to use (espeak, festival, etc.).
    /// </summary>
    public string Engine { get; init; } = "espeak";

    /// <summary>
    /// Voice identifier for the TTS engine.
    /// </summary>
    public string Voice { get; init; } = "en";

    /// <summary>
    /// Speech rate in words per minute.
    /// </summary>
    public int Rate { get; init; } = 150;

    /// <summary>
    /// Pitch adjustment (-100 to 100, 0 is default).
    /// </summary>
    public int Pitch { get; init; } = 0;

    /// <summary>
    /// Volume level (0-200, 100 is default).
    /// </summary>
    public int Volume { get; init; } = 100;
}
