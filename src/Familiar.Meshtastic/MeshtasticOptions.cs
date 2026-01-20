namespace Familiar.Meshtastic;

/// <summary>
/// Configuration options for Meshtastic integration.
/// </summary>
public class MeshtasticOptions
{
    public const string SectionName = "Familiar:Meshtastic";

    /// <summary>
    /// Whether Meshtastic integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Serial port for Meshtastic device (e.g., /dev/ttyUSB0).
    /// </summary>
    public string Port { get; set; } = "/dev/ttyUSB0";

    /// <summary>
    /// Baud rate for serial communication.
    /// </summary>
    public int BaudRate { get; set; } = 115200;

    /// <summary>
    /// Node name to display on the mesh network.
    /// </summary>
    public string NodeName { get; set; } = "Familiar";

    /// <summary>
    /// Channel index to listen on (0 = primary).
    /// </summary>
    public int Channel { get; set; } = 0;

    /// <summary>
    /// List of allowed sender node IDs. Empty = allow all.
    /// </summary>
    public List<string> AllowedNodes { get; set; } = new();

    /// <summary>
    /// Reconnection delay in seconds after connection loss.
    /// </summary>
    public int ReconnectDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Timeout in milliseconds for device configuration sequence.
    /// </summary>
    public int ConfigTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Heartbeat interval in milliseconds (0 = disabled).
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 0;
}
