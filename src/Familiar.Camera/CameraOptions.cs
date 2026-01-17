namespace Familiar.Camera;

/// <summary>
/// Configuration options for Pi Camera (Pi 5 only).
/// </summary>
public class CameraOptions
{
    public const string SectionName = "Familiar:Camera";

    /// <summary>
    /// Whether camera features are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Video width in pixels.
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// Video height in pixels.
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// Frame rate for video capture.
    /// </summary>
    public int Framerate { get; set; } = 30;

    /// <summary>
    /// Directory path for storing recordings.
    /// </summary>
    public string RecordingPath { get; set; } = "/home/familiar/recordings";

    /// <summary>
    /// Bitrate for streaming in bits per second.
    /// </summary>
    public int StreamBitrate { get; set; } = 4_000_000; // 4 Mbps

    /// <summary>
    /// Bitrate for recording in bits per second.
    /// </summary>
    public int RecordingBitrate { get; set; } = 8_000_000; // 8 Mbps

    /// <summary>
    /// JPEG quality for snapshots (1-100).
    /// </summary>
    public int SnapshotQuality { get; set; } = 90;
}
