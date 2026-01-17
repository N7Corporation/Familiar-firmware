namespace Familiar.Audio;

/// <summary>
/// Interface for audio capture (microphone) operations.
/// </summary>
public interface IAudioCapture : IDisposable
{
    /// <summary>
    /// Gets whether audio capture is currently active.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Starts capturing audio from the microphone.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops capturing audio.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Reads captured audio frames as an async enumerable.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of audio frame byte arrays.</returns>
    IAsyncEnumerable<byte[]> ReadFramesAsync(CancellationToken ct = default);
}
