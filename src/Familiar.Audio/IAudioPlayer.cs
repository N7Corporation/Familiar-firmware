namespace Familiar.Audio;

/// <summary>
/// Interface for audio playback operations.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>
    /// Gets whether the player is currently playing audio.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Initializes the audio player with the configured device.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Writes audio data to the playback buffer.
    /// </summary>
    /// <param name="audioData">PCM audio data to play.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);

    /// <summary>
    /// Sets the output volume.
    /// </summary>
    /// <param name="level">Volume level from 0.0 to 1.0.</param>
    void SetVolume(float level);

    /// <summary>
    /// Stops playback and clears the buffer.
    /// </summary>
    Task StopAsync();
}
