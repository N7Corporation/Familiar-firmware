namespace Familiar.Meshtastic;

/// <summary>
/// Represents a message received from the Meshtastic network.
/// </summary>
public record MeshtasticMessage
{
    /// <summary>
    /// The node ID that sent the message.
    /// </summary>
    public required string FromNode { get; init; }

    /// <summary>
    /// The node ID the message was sent to (may be broadcast).
    /// </summary>
    public required string ToNode { get; init; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The channel index the message was received on.
    /// </summary>
    public int Channel { get; init; }

    /// <summary>
    /// UTC timestamp when the message was received.
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Signal-to-noise ratio of the received message.
    /// </summary>
    public float? Snr { get; init; }

    /// <summary>
    /// RSSI (received signal strength indicator) in dBm.
    /// </summary>
    public int? Rssi { get; init; }
}

/// <summary>
/// Represents a node on the Meshtastic network.
/// </summary>
public record MeshtasticNode
{
    /// <summary>
    /// Unique node identifier.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// User-friendly node name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Short name (4 chars).
    /// </summary>
    public string? ShortName { get; init; }

    /// <summary>
    /// Last time this node was heard from.
    /// </summary>
    public DateTime? LastHeard { get; init; }

    /// <summary>
    /// Battery level percentage (0-100).
    /// </summary>
    public int? BatteryLevel { get; init; }
}
