using Google.Protobuf;
using Meshtastic.Protobufs;
using Microsoft.Extensions.Logging;

namespace Familiar.Meshtastic.Protocol;

/// <summary>
/// Handles serialization and deserialization of Meshtastic protobuf messages.
/// </summary>
public class MessageSerializer
{
    private readonly ILogger? _logger;

    public MessageSerializer(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Serializes a ToRadio message to bytes.
    /// </summary>
    /// <param name="message">The ToRadio message to serialize.</param>
    /// <returns>Serialized bytes.</returns>
    public byte[] SerializeToRadio(ToRadio message)
    {
        return message.ToByteArray();
    }

    /// <summary>
    /// Deserializes a FromRadio message from bytes.
    /// </summary>
    /// <param name="data">The bytes to deserialize.</param>
    /// <returns>The deserialized FromRadio message, or null if deserialization fails.</returns>
    public FromRadio? DeserializeFromRadio(byte[] data)
    {
        try
        {
            return FromRadio.Parser.ParseFrom(data);
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger?.LogWarning(ex, "Failed to deserialize FromRadio message");
            return null;
        }
    }

    /// <summary>
    /// Creates a ToRadio message with a want_config_id request.
    /// </summary>
    /// <param name="configId">The config request ID to use.</param>
    /// <returns>A ToRadio message.</returns>
    public static ToRadio CreateWantConfigRequest(uint configId)
    {
        return new ToRadio
        {
            WantConfigId = configId
        };
    }

    /// <summary>
    /// Creates a ToRadio message with a heartbeat.
    /// </summary>
    /// <returns>A ToRadio message.</returns>
    public static ToRadio CreateHeartbeat()
    {
        return new ToRadio
        {
            Heartbeat = new Heartbeat()
        };
    }

    /// <summary>
    /// Creates a ToRadio message containing a MeshPacket.
    /// </summary>
    /// <param name="packet">The MeshPacket to send.</param>
    /// <returns>A ToRadio message.</returns>
    public static ToRadio CreatePacketMessage(MeshPacket packet)
    {
        return new ToRadio
        {
            Packet = packet
        };
    }

    /// <summary>
    /// Creates a MeshPacket for sending a text message.
    /// </summary>
    /// <param name="text">The text message to send.</param>
    /// <param name="destination">The destination node (0xFFFFFFFF for broadcast).</param>
    /// <param name="channel">The channel index.</param>
    /// <param name="wantAck">Whether to request acknowledgment.</param>
    /// <returns>A MeshPacket ready to send.</returns>
    public static MeshPacket CreateTextMessagePacket(
        string text,
        uint destination = FrameConstants.BroadcastAddress,
        uint channel = 0,
        bool wantAck = false)
    {
        var data = new Data
        {
            Portnum = PortNum.TextMessageApp,
            Payload = ByteString.CopyFromUtf8(text)
        };

        return new MeshPacket
        {
            To = destination,
            Channel = channel,
            WantAck = wantAck,
            Decoded = data,
            Priority = MeshPacketPriority.PriorityDefault
        };
    }

    /// <summary>
    /// Extracts text from a Data payload if it's a text message.
    /// </summary>
    /// <param name="data">The Data payload.</param>
    /// <returns>The text content, or null if not a text message.</returns>
    public static string? ExtractTextMessage(Data data)
    {
        if (data.Portnum != PortNum.TextMessageApp)
        {
            return null;
        }

        return data.Payload.ToStringUtf8();
    }

    /// <summary>
    /// Extracts telemetry from a Data payload if present.
    /// </summary>
    /// <param name="data">The Data payload.</param>
    /// <returns>The Telemetry message, or null if not telemetry.</returns>
    public Telemetry? ExtractTelemetry(Data data)
    {
        if (data.Portnum != PortNum.TelemetryApp)
        {
            return null;
        }

        try
        {
            return Telemetry.Parser.ParseFrom(data.Payload);
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse telemetry payload");
            return null;
        }
    }

    /// <summary>
    /// Extracts position from a Data payload if present.
    /// </summary>
    /// <param name="data">The Data payload.</param>
    /// <returns>The Position message, or null if not position data.</returns>
    public Position? ExtractPosition(Data data)
    {
        if (data.Portnum != PortNum.PositionApp)
        {
            return null;
        }

        try
        {
            return Position.Parser.ParseFrom(data.Payload);
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse position payload");
            return null;
        }
    }

    /// <summary>
    /// Extracts node info from a Data payload if present.
    /// </summary>
    /// <param name="data">The Data payload.</param>
    /// <returns>The User message (node info), or null if not node info.</returns>
    public User? ExtractNodeInfo(Data data)
    {
        if (data.Portnum != PortNum.NodeinfoApp)
        {
            return null;
        }

        try
        {
            return User.Parser.ParseFrom(data.Payload);
        }
        catch (InvalidProtocolBufferException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse node info payload");
            return null;
        }
    }
}
