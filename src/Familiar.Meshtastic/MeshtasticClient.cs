using Familiar.Meshtastic.Connection;
using Familiar.Meshtastic.Protocol;
using Meshtastic.Protobufs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Meshtastic;

/// <summary>
/// Client for communicating with a Meshtastic device over serial port.
/// Uses the binary protobuf protocol for full device communication.
/// </summary>
public class MeshtasticClient : IMeshtasticClient
{
    private readonly MeshtasticOptions _options;
    private readonly ILogger<MeshtasticClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private MeshtasticConnection? _connection;
    private readonly List<MeshtasticNode> _knownNodes = new();
    private readonly object _nodesLock = new();
    private bool _disposed;

    public MeshtasticClient(
        IOptions<MeshtasticOptions> options,
        ILogger<MeshtasticClient> logger,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public bool IsConnected => _connection?.IsConnected == true;

    public ConnectionState ConnectionState => _connection?.State ?? Connection.ConnectionState.Disconnected;

    public IReadOnlyList<MeshtasticNode> KnownNodes
    {
        get
        {
            lock (_nodesLock)
            {
                return _knownNodes.ToList().AsReadOnly();
            }
        }
    }

    public uint? MyNodeNum => _connection?.MyNodeInfo?.MyNodeNum;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<NodeUpdatedEventArgs>? NodeUpdated;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected)
        {
            return;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation("Meshtastic integration is disabled");
            return;
        }

        // Create connection
        _connection = new MeshtasticConnection(
            _options,
            _loggerFactory.CreateLogger<MeshtasticConnection>());

        // Wire up events
        _connection.StateChanged += OnConnectionStateChanged;
        _connection.Dispatcher.PacketReceived += OnPacketReceived;
        _connection.Dispatcher.NodeInfoReceived += OnNodeInfoReceived;

        // Connect
        var connected = await _connection.ConnectAsync(ct);

        if (connected)
        {
            _logger.LogInformation("Connected to Meshtastic device on {Port}", _options.Port);

            // Populate known nodes from device's node database
            PopulateKnownNodesFromConnection();
        }
        else
        {
            _logger.LogError("Failed to connect to Meshtastic device on {Port}", _options.Port);
            await CleanupConnection();
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.DisconnectAsync();
            await CleanupConnection();
            _logger.LogInformation("Disconnected from Meshtastic device");
        }
    }

    public async Task SendMessageAsync(string text, string? destinationNode = null, CancellationToken ct = default)
    {
        if (_connection == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to Meshtastic device");
        }

        uint destination = FrameConstants.BroadcastAddress;

        if (!string.IsNullOrEmpty(destinationNode))
        {
            destination = ParseNodeId(destinationNode);
        }

        await _connection.SendTextMessageAsync(
            text,
            destination,
            (uint)_options.Channel,
            wantAck: false,
            ct);

        _logger.LogDebug("Sent message to {Destination}: {Text}",
            destinationNode ?? "broadcast", text);
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        var isConnected = e.NewState == Connection.ConnectionState.Connected;
        var wasConnected = e.OldState == Connection.ConnectionState.Connected;

        if (isConnected != wasConnected)
        {
            ConnectionStateChanged?.Invoke(this, isConnected);
        }
    }

    private void OnPacketReceived(object? sender, MeshPacketReceivedEventArgs e)
    {
        var packet = e.Packet;

        // Check if this is a decoded packet with text message
        if (packet.PayloadVariantCase == MeshPacket.PayloadVariantOneofCase.Decoded)
        {
            var data = packet.Decoded;
            var text = MessageSerializer.ExtractTextMessage(data);

            if (text != null)
            {
                var message = CreateMeshtasticMessage(packet, text);

                if (ShouldProcessMessage(message))
                {
                    _logger.LogInformation("Message from {Node}: {Text}",
                        message.FromNode, message.Text);

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                }
            }
        }
    }

    private void OnNodeInfoReceived(object? sender, NodeInfoReceivedEventArgs e)
    {
        var node = ConvertToMeshtasticNode(e.NodeInfo);
        UpdateKnownNode(node);
        NodeUpdated?.Invoke(this, new NodeUpdatedEventArgs(node));
    }

    private MeshtasticMessage CreateMeshtasticMessage(MeshPacket packet, string text)
    {
        var fromNodeId = FormatNodeId(packet.From);
        var toNodeId = packet.To == FrameConstants.BroadcastAddress
            ? "broadcast"
            : FormatNodeId(packet.To);

        // Try to get position from sender's node info
        double? latitude = null;
        double? longitude = null;
        int? altitude = null;

        if (_connection?.NodeDatabase.TryGetValue(packet.From, out var nodeInfo) == true)
        {
            var pos = nodeInfo.Position;
            if (pos != null && pos.LatitudeI != 0)
            {
                latitude = pos.LatitudeI / 1e7;
                longitude = pos.LongitudeI / 1e7;
                altitude = pos.Altitude;
            }
        }

        return new MeshtasticMessage
        {
            FromNode = fromNodeId,
            ToNode = toNodeId,
            FromNodeNum = packet.From,
            ToNodeNum = packet.To,
            Text = text,
            Channel = (int)packet.Channel,
            ReceivedAt = DateTime.UtcNow,
            Snr = packet.RxSnr,
            Rssi = packet.RxRssi,
            PacketId = packet.Id,
            HopLimit = packet.HopLimit,
            HopStart = packet.HopStart,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = altitude
        };
    }

    private MeshtasticNode ConvertToMeshtasticNode(NodeInfo nodeInfo)
    {
        var nodeId = FormatNodeId(nodeInfo.Num);

        // Extract position
        double? latitude = null;
        double? longitude = null;
        int? altitude = null;

        if (nodeInfo.Position != null && nodeInfo.Position.LatitudeI != 0)
        {
            latitude = nodeInfo.Position.LatitudeI / 1e7;
            longitude = nodeInfo.Position.LongitudeI / 1e7;
            altitude = nodeInfo.Position.Altitude;
        }

        // Extract device metrics
        int? batteryLevel = null;
        float? voltage = null;
        float? channelUtil = null;
        float? airUtilTx = null;
        uint? uptimeSeconds = null;

        if (nodeInfo.DeviceMetrics != null)
        {
            batteryLevel = (int)nodeInfo.DeviceMetrics.BatteryLevel;
            voltage = nodeInfo.DeviceMetrics.Voltage;
            channelUtil = nodeInfo.DeviceMetrics.ChannelUtilization;
            airUtilTx = nodeInfo.DeviceMetrics.AirUtilTx;
            uptimeSeconds = nodeInfo.DeviceMetrics.UptimeSeconds;
        }

        // Convert last heard
        DateTime? lastHeard = null;
        if (nodeInfo.LastHeard > 0)
        {
            lastHeard = DateTimeOffset.FromUnixTimeSeconds(nodeInfo.LastHeard).UtcDateTime;
        }

        return new MeshtasticNode
        {
            NodeId = nodeId,
            NodeNum = nodeInfo.Num,
            Name = nodeInfo.User?.LongName,
            ShortName = nodeInfo.User?.ShortName,
            LastHeard = lastHeard,
            BatteryLevel = batteryLevel,
            Voltage = voltage,
            ChannelUtilization = channelUtil,
            AirUtilTx = airUtilTx,
            UptimeSeconds = uptimeSeconds,
            Snr = nodeInfo.Snr,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = altitude,
            HardwareModel = nodeInfo.User?.HwModel.ToString(),
            HopsAway = nodeInfo.HopsAway,
            ViaMqtt = nodeInfo.ViaMqtt,
            IsFavorite = nodeInfo.IsFavorite
        };
    }

    private void PopulateKnownNodesFromConnection()
    {
        if (_connection == null) return;

        lock (_nodesLock)
        {
            _knownNodes.Clear();

            foreach (var kvp in _connection.NodeDatabase)
            {
                var node = ConvertToMeshtasticNode(kvp.Value);
                _knownNodes.Add(node);
            }
        }

        _logger.LogDebug("Populated {Count} nodes from device database", _knownNodes.Count);
    }

    private void UpdateKnownNode(MeshtasticNode node)
    {
        lock (_nodesLock)
        {
            var existingIndex = _knownNodes.FindIndex(n => n.NodeNum == node.NodeNum);
            if (existingIndex >= 0)
            {
                _knownNodes[existingIndex] = node;
            }
            else
            {
                _knownNodes.Add(node);
            }
        }
    }

    private bool ShouldProcessMessage(MeshtasticMessage message)
    {
        if (_options.AllowedNodes.Count > 0 &&
            !_options.AllowedNodes.Contains(message.FromNode))
        {
            _logger.LogDebug("Ignoring message from non-allowed node: {Node}", message.FromNode);
            return false;
        }

        return true;
    }

    private async Task CleanupConnection()
    {
        if (_connection != null)
        {
            _connection.StateChanged -= OnConnectionStateChanged;
            _connection.Dispatcher.PacketReceived -= OnPacketReceived;
            _connection.Dispatcher.NodeInfoReceived -= OnNodeInfoReceived;
            _connection.Dispose();
            _connection = null;
        }

        lock (_nodesLock)
        {
            _knownNodes.Clear();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Formats a node number as a hex string with ! prefix.
    /// </summary>
    private static string FormatNodeId(uint nodeNum)
    {
        return $"!{nodeNum:x8}";
    }

    /// <summary>
    /// Parses a node ID string to a uint.
    /// </summary>
    private static uint ParseNodeId(string nodeId)
    {
        var hex = nodeId.TrimStart('!');
        return Convert.ToUInt32(hex, 16);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore errors during disposal
        }
        GC.SuppressFinalize(this);
    }
}
