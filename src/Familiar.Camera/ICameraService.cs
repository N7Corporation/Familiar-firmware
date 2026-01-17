namespace Familiar.Camera;

/// <summary>
/// Interface for Pi Camera operations (Pi 5 only).
/// </summary>
public interface ICameraService : IDisposable
{
    /// <summary>
    /// Gets whether a camera is available on the system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets whether the camera is currently streaming.
    /// </summary>
    bool IsStreaming { get; }

    /// <summary>
    /// Gets whether the camera is currently recording.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Gets the current recording filename, if recording.
    /// </summary>
    string? CurrentRecordingFile { get; }

    /// <summary>
    /// Starts video streaming.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if streaming started successfully.</returns>
    Task<bool> StartStreamingAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops video streaming.
    /// </summary>
    Task StopStreamingAsync();

    /// <summary>
    /// Starts recording video to a file.
    /// </summary>
    /// <param name="filename">Base filename (without extension).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if recording started successfully.</returns>
    Task<bool> StartRecordingAsync(string filename, CancellationToken ct = default);

    /// <summary>
    /// Stops recording and returns the full path to the recorded file.
    /// </summary>
    /// <returns>Full path to the recorded file, or null if not recording.</returns>
    Task<string?> StopRecordingAsync();

    /// <summary>
    /// Captures a single snapshot.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JPEG image data, or null on failure.</returns>
    Task<byte[]?> CaptureSnapshotAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets video stream frames as an async enumerable.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of H.264 encoded frames.</returns>
    IAsyncEnumerable<byte[]> GetStreamFramesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a list of recorded video files.
    /// </summary>
    /// <returns>List of recording file information.</returns>
    Task<IReadOnlyList<RecordingInfo>> GetRecordingsAsync();

    /// <summary>
    /// Deletes a recording file.
    /// </summary>
    /// <param name="filename">Filename to delete.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteRecordingAsync(string filename);
}

/// <summary>
/// Information about a recorded video file.
/// </summary>
public record RecordingInfo
{
    /// <summary>
    /// Filename (without path).
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// Full path to the file.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
