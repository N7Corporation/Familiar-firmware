namespace Familiar.Tts;

/// <summary>
/// Interface for text-to-speech engines.
/// </summary>
public interface ITtsEngine
{
    /// <summary>
    /// Gets whether the TTS engine is available on the system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Synthesizes text to audio data.
    /// </summary>
    /// <param name="text">Text to synthesize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PCM audio data (16-bit, mono, 48kHz).</returns>
    Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Sets the voice to use for synthesis.
    /// </summary>
    /// <param name="voiceId">Voice identifier.</param>
    void SetVoice(string voiceId);

    /// <summary>
    /// Sets the speech rate.
    /// </summary>
    /// <param name="wordsPerMinute">Speech rate in words per minute.</param>
    void SetRate(int wordsPerMinute);

    /// <summary>
    /// Gets available voices for this engine.
    /// </summary>
    /// <returns>List of available voice identifiers.</returns>
    Task<IReadOnlyList<string>> GetAvailableVoicesAsync(CancellationToken ct = default);
}
