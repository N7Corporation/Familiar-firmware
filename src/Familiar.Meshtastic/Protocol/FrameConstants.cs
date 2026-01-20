namespace Familiar.Meshtastic.Protocol;

/// <summary>
/// Constants for Meshtastic serial frame protocol.
/// </summary>
public static class FrameConstants
{
    /// <summary>
    /// First magic byte of the frame header (0x94).
    /// </summary>
    public const byte Magic1 = 0x94;

    /// <summary>
    /// Second magic byte of the frame header (0xC3).
    /// </summary>
    public const byte Magic2 = 0xC3;

    /// <summary>
    /// Size of the frame header (magic bytes + length).
    /// </summary>
    public const int HeaderSize = 4;

    /// <summary>
    /// Maximum payload size in bytes.
    /// </summary>
    public const int MaxPayloadSize = 512;

    /// <summary>
    /// Maximum total frame size (header + payload).
    /// </summary>
    public const int MaxFrameSize = HeaderSize + MaxPayloadSize;

    /// <summary>
    /// Default read timeout in milliseconds.
    /// </summary>
    public const int DefaultReadTimeoutMs = 5000;

    /// <summary>
    /// Broadcast destination address.
    /// </summary>
    public const uint BroadcastAddress = 0xFFFFFFFF;
}
