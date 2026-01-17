namespace Familiar.Audio;

/// <summary>
/// Central interface for managing all audio operations.
/// Handles both playback (handler → cosplayer) and capture (cosplayer → handler).
/// </summary>
public interface IAudioManager : IDisposable
{
    #region Playback (Handler → Cosplayer)

    /// <summary>
    /// Plays incoming audio stream data through the speaker.
    /// </summary>
    /// <param name="audioData">PCM audio data to play.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PlayStreamAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);

    /// <summary>
    /// Converts text to speech and plays it.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="priority">Priority level (higher = more important).</param>
    /// <param name="ct">Cancellation token.</param>
    Task PlayTtsAsync(string text, int priority = 0, CancellationToken ct = default);

    /// <summary>
    /// Sets the output volume level.
    /// </summary>
    /// <param name="level">Volume from 0.0 to 1.0.</param>
    void SetVolume(float level);

    /// <summary>
    /// Gets the current volume level.
    /// </summary>
    float Volume { get; }

    /// <summary>
    /// Gets or sets whether audio output is muted.
    /// </summary>
    bool IsMuted { get; set; }

    #endregion

    #region Capture (Cosplayer → Handler)

    /// <summary>
    /// Gets whether microphone capture is active.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Starts capturing audio from the microphone.
    /// </summary>
    Task StartCaptureAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops capturing audio.
    /// </summary>
    Task StopCaptureAsync();

    /// <summary>
    /// Gets captured audio frames as an async enumerable.
    /// Only yields frames when voice is detected (VOX mode) or PTT is active.
    /// </summary>
    IAsyncEnumerable<byte[]> GetCapturedAudioAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets or sets the microphone mode ("vox" or "ptt").
    /// </summary>
    string MicMode { get; set; }

    /// <summary>
    /// Sets the PTT (push-to-talk) state. Only used when MicMode is "ptt".
    /// </summary>
    bool PttActive { get; set; }

    #endregion
}
