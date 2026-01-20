using Meshtastic.Protobufs;
using Microsoft.Extensions.Logging;

namespace Familiar.Meshtastic.Protocol;

/// <summary>
/// Event arguments for when a mesh packet is received.
/// </summary>
public class MeshPacketReceivedEventArgs : EventArgs
{
    public MeshPacketReceivedEventArgs(MeshPacket packet)
    {
        Packet = packet;
    }

    public MeshPacket Packet { get; }
}

/// <summary>
/// Event arguments for when node info is received.
/// </summary>
public class NodeInfoReceivedEventArgs : EventArgs
{
    public NodeInfoReceivedEventArgs(NodeInfo nodeInfo)
    {
        NodeInfo = nodeInfo;
    }

    public NodeInfo NodeInfo { get; }
}

/// <summary>
/// Event arguments for when my node info is received.
/// </summary>
public class MyNodeInfoReceivedEventArgs : EventArgs
{
    public MyNodeInfoReceivedEventArgs(MyNodeInfo myInfo)
    {
        MyInfo = myInfo;
    }

    public MyNodeInfo MyInfo { get; }
}

/// <summary>
/// Event arguments for when config is complete.
/// </summary>
public class ConfigCompleteEventArgs : EventArgs
{
    public ConfigCompleteEventArgs(uint configId)
    {
        ConfigId = configId;
    }

    public uint ConfigId { get; }
}

/// <summary>
/// Event arguments for when channel info is received.
/// </summary>
public class ChannelReceivedEventArgs : EventArgs
{
    public ChannelReceivedEventArgs(Channel channel)
    {
        Channel = channel;
    }

    public Channel Channel { get; }
}

/// <summary>
/// Event arguments for when config is received.
/// </summary>
public class ConfigReceivedEventArgs : EventArgs
{
    public ConfigReceivedEventArgs(Config config)
    {
        Config = config;
    }

    public Config Config { get; }
}

/// <summary>
/// Event arguments for when module config is received.
/// </summary>
public class ModuleConfigReceivedEventArgs : EventArgs
{
    public ModuleConfigReceivedEventArgs(ModuleConfig moduleConfig)
    {
        ModuleConfig = moduleConfig;
    }

    public ModuleConfig ModuleConfig { get; }
}

/// <summary>
/// Event arguments for device metadata.
/// </summary>
public class DeviceMetadataReceivedEventArgs : EventArgs
{
    public DeviceMetadataReceivedEventArgs(DeviceMetadata metadata)
    {
        Metadata = metadata;
    }

    public DeviceMetadata Metadata { get; }
}

/// <summary>
/// Dispatches FromRadio messages to appropriate event handlers.
/// </summary>
public class FromRadioDispatcher
{
    private readonly ILogger? _logger;

    public FromRadioDispatcher(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Raised when a mesh packet is received.
    /// </summary>
    public event EventHandler<MeshPacketReceivedEventArgs>? PacketReceived;

    /// <summary>
    /// Raised when node info is received (from node database).
    /// </summary>
    public event EventHandler<NodeInfoReceivedEventArgs>? NodeInfoReceived;

    /// <summary>
    /// Raised when my node info is received.
    /// </summary>
    public event EventHandler<MyNodeInfoReceivedEventArgs>? MyInfoReceived;

    /// <summary>
    /// Raised when configuration request is complete.
    /// </summary>
    public event EventHandler<ConfigCompleteEventArgs>? ConfigComplete;

    /// <summary>
    /// Raised when a channel is received.
    /// </summary>
    public event EventHandler<ChannelReceivedEventArgs>? ChannelReceived;

    /// <summary>
    /// Raised when device config is received.
    /// </summary>
    public event EventHandler<ConfigReceivedEventArgs>? ConfigReceived;

    /// <summary>
    /// Raised when module config is received.
    /// </summary>
    public event EventHandler<ModuleConfigReceivedEventArgs>? ModuleConfigReceived;

    /// <summary>
    /// Raised when device metadata is received.
    /// </summary>
    public event EventHandler<DeviceMetadataReceivedEventArgs>? MetadataReceived;

    /// <summary>
    /// Raised when the device has rebooted.
    /// </summary>
    public event EventHandler? DeviceRebooted;

    /// <summary>
    /// Dispatches a FromRadio message to the appropriate event handler.
    /// </summary>
    /// <param name="message">The FromRadio message to dispatch.</param>
    public void Dispatch(FromRadio message)
    {
        _logger?.LogTrace("Dispatching FromRadio id={Id}, type={Type}",
            message.Id, message.PayloadVariantCase);

        switch (message.PayloadVariantCase)
        {
            case FromRadio.PayloadVariantOneofCase.Packet:
                OnPacketReceived(message.Packet);
                break;

            case FromRadio.PayloadVariantOneofCase.MyInfo:
                OnMyInfoReceived(message.MyInfo);
                break;

            case FromRadio.PayloadVariantOneofCase.NodeInfo:
                OnNodeInfoReceived(message.NodeInfo);
                break;

            case FromRadio.PayloadVariantOneofCase.Config:
                OnConfigReceived(message.Config);
                break;

            case FromRadio.PayloadVariantOneofCase.ModuleConfig:
                OnModuleConfigReceived(message.ModuleConfig);
                break;

            case FromRadio.PayloadVariantOneofCase.Channel:
                OnChannelReceived(message.Channel);
                break;

            case FromRadio.PayloadVariantOneofCase.ConfigCompleteId:
                OnConfigComplete(message.ConfigCompleteId);
                break;

            case FromRadio.PayloadVariantOneofCase.Rebooted:
                if (message.Rebooted)
                {
                    OnDeviceRebooted();
                }
                break;

            case FromRadio.PayloadVariantOneofCase.Metadata:
                OnMetadataReceived(message.Metadata);
                break;

            case FromRadio.PayloadVariantOneofCase.LogRecord:
                _logger?.LogDebug("[Device] {Level}: {Message}",
                    message.LogRecord.Level, message.LogRecord.Message);
                break;

            case FromRadio.PayloadVariantOneofCase.QueueStatus:
                _logger?.LogTrace("Queue status: free={Free}, maxlen={MaxLen}",
                    message.QueueStatus.Free, message.QueueStatus.Maxlen);
                break;

            case FromRadio.PayloadVariantOneofCase.None:
                _logger?.LogWarning("Received FromRadio with no payload");
                break;

            default:
                _logger?.LogDebug("Unhandled FromRadio payload type: {Type}",
                    message.PayloadVariantCase);
                break;
        }
    }

    private void OnPacketReceived(MeshPacket packet)
    {
        _logger?.LogTrace("Received packet from={From:X8}, to={To:X8}, port={Port}",
            packet.From, packet.To,
            packet.Decoded?.Portnum.ToString() ?? "encrypted");

        PacketReceived?.Invoke(this, new MeshPacketReceivedEventArgs(packet));
    }

    private void OnMyInfoReceived(MyNodeInfo myInfo)
    {
        _logger?.LogDebug("My node info: num={Num:X8}", myInfo.MyNodeNum);
        MyInfoReceived?.Invoke(this, new MyNodeInfoReceivedEventArgs(myInfo));
    }

    private void OnNodeInfoReceived(NodeInfo nodeInfo)
    {
        _logger?.LogDebug("Node info: num={Num:X8}, name={Name}",
            nodeInfo.Num, nodeInfo.User?.LongName ?? "unknown");
        NodeInfoReceived?.Invoke(this, new NodeInfoReceivedEventArgs(nodeInfo));
    }

    private void OnConfigReceived(Config config)
    {
        _logger?.LogTrace("Received config");
        ConfigReceived?.Invoke(this, new ConfigReceivedEventArgs(config));
    }

    private void OnModuleConfigReceived(ModuleConfig moduleConfig)
    {
        _logger?.LogTrace("Received module config");
        ModuleConfigReceived?.Invoke(this, new ModuleConfigReceivedEventArgs(moduleConfig));
    }

    private void OnChannelReceived(Channel channel)
    {
        _logger?.LogDebug("Channel: index={Index}, role={Role}, name={Name}",
            channel.Index, channel.Role, channel.Settings?.Name ?? "(unnamed)");
        ChannelReceived?.Invoke(this, new ChannelReceivedEventArgs(channel));
    }

    private void OnConfigComplete(uint configId)
    {
        _logger?.LogDebug("Config complete: id={Id}", configId);
        ConfigComplete?.Invoke(this, new ConfigCompleteEventArgs(configId));
    }

    private void OnDeviceRebooted()
    {
        _logger?.LogInformation("Device rebooted");
        DeviceRebooted?.Invoke(this, EventArgs.Empty);
    }

    private void OnMetadataReceived(DeviceMetadata metadata)
    {
        _logger?.LogDebug("Device metadata: firmware={Firmware}, hw={HwModel}",
            metadata.FirmwareVersion, metadata.HwModel);
        MetadataReceived?.Invoke(this, new DeviceMetadataReceivedEventArgs(metadata));
    }
}
