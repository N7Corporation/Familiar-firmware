namespace Familiar.Meshtastic;

/// <summary>
/// Represents a message received from the Meshtastic network.
/// </summary>
public record MeshtasticMessage
{
    /// <summary>
    /// The node ID that sent the message (hex string with ! prefix).
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

    /// <summary>
    /// Unique packet identifier.
    /// </summary>
    public uint? PacketId { get; init; }

    /// <summary>
    /// Hop limit (remaining hops before packet is dropped).
    /// </summary>
    public uint? HopLimit { get; init; }

    /// <summary>
    /// Original hop limit when packet was sent.
    /// </summary>
    public uint? HopStart { get; init; }

    /// <summary>
    /// Sender's latitude (if position enabled).
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Sender's longitude (if position enabled).
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Sender's altitude in meters (if position enabled).
    /// </summary>
    public int? Altitude { get; init; }

    /// <summary>
    /// Source node number (raw uint32).
    /// </summary>
    public uint FromNodeNum { get; init; }

    /// <summary>
    /// Destination node number (raw uint32).
    /// </summary>
    public uint ToNodeNum { get; init; }
}

/// <summary>
/// Represents a node on the Meshtastic network.
/// </summary>
public record MeshtasticNode
{
    /// <summary>
    /// Unique node identifier (hex string with ! prefix).
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// Raw node number (uint32).
    /// </summary>
    public uint NodeNum { get; init; }

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
    /// Battery level percentage (0-100, >100 = powered).
    /// </summary>
    public int? BatteryLevel { get; init; }

    /// <summary>
    /// Battery voltage.
    /// </summary>
    public float? Voltage { get; init; }

    /// <summary>
    /// Channel utilization percentage.
    /// </summary>
    public float? ChannelUtilization { get; init; }

    /// <summary>
    /// Airtime utilization percentage (TX).
    /// </summary>
    public float? AirUtilTx { get; init; }

    /// <summary>
    /// Device uptime in seconds.
    /// </summary>
    public uint? UptimeSeconds { get; init; }

    /// <summary>
    /// Signal-to-noise ratio of last received packet.
    /// </summary>
    public float? Snr { get; init; }

    /// <summary>
    /// Latitude.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Altitude in meters.
    /// </summary>
    public int? Altitude { get; init; }

    /// <summary>
    /// Hardware model.
    /// </summary>
    public string? HardwareModel { get; init; }

    /// <summary>
    /// Number of hops away.
    /// </summary>
    public uint? HopsAway { get; init; }

    /// <summary>
    /// Whether this node was received via MQTT.
    /// </summary>
    public bool ViaMqtt { get; init; }

    /// <summary>
    /// Whether this is a favorite node.
    /// </summary>
    public bool IsFavorite { get; init; }
}
