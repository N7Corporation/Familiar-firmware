using Familiar.Meshtastic.Protocol;
using FluentAssertions;
using Meshtastic.Protobufs;
using Xunit;

namespace Familiar.Meshtastic.Tests.Protocol;

/// <summary>
/// Unit tests for FromRadioDispatcher.
/// </summary>
public class FromRadioDispatcherTests
{
    private readonly FromRadioDispatcher _dispatcher;

    public FromRadioDispatcherTests()
    {
        _dispatcher = new FromRadioDispatcher();
    }

    #region Packet Dispatch Tests

    [Fact]
    public void Dispatch_MeshPacket_RaisesPacketReceivedEvent()
    {
        // Arrange
        MeshPacket? receivedPacket = null;
        _dispatcher.PacketReceived += (sender, e) => receivedPacket = e.Packet;

        var packet = new MeshPacket
        {
            From = 0x12345678,
            To = 0xFFFFFFFF,
            Id = 123
        };

        var fromRadio = new FromRadio
        {
            Id = 1,
            Packet = packet
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedPacket.Should().NotBeNull();
        receivedPacket!.From.Should().Be(0x12345678);
        receivedPacket.To.Should().Be(0xFFFFFFFF);
        receivedPacket.Id.Should().Be(123);
    }

    [Fact]
    public void Dispatch_NodeInfo_RaisesNodeInfoReceivedEvent()
    {
        // Arrange
        NodeInfo? receivedNodeInfo = null;
        _dispatcher.NodeInfoReceived += (sender, e) => receivedNodeInfo = e.NodeInfo;

        var nodeInfo = new NodeInfo
        {
            Num = 0xABCDEF01,
            User = new User
            {
                LongName = "Test Node",
                ShortName = "TEST"
            }
        };

        var fromRadio = new FromRadio
        {
            Id = 2,
            NodeInfo = nodeInfo
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedNodeInfo.Should().NotBeNull();
        receivedNodeInfo!.Num.Should().Be(0xABCDEF01);
        receivedNodeInfo.User.LongName.Should().Be("Test Node");
        receivedNodeInfo.User.ShortName.Should().Be("TEST");
    }

    [Fact]
    public void Dispatch_MyInfo_RaisesMyInfoReceivedEvent()
    {
        // Arrange
        MyNodeInfo? receivedMyInfo = null;
        _dispatcher.MyInfoReceived += (sender, e) => receivedMyInfo = e.MyInfo;

        var myInfo = new MyNodeInfo
        {
            MyNodeNum = 0x11223344,
            RebootCount = 5
        };

        var fromRadio = new FromRadio
        {
            Id = 3,
            MyInfo = myInfo
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedMyInfo.Should().NotBeNull();
        receivedMyInfo!.MyNodeNum.Should().Be(0x11223344);
        receivedMyInfo.RebootCount.Should().Be(5);
    }

    [Fact]
    public void Dispatch_ConfigComplete_RaisesConfigCompleteEvent()
    {
        // Arrange
        uint? receivedConfigId = null;
        _dispatcher.ConfigComplete += (sender, e) => receivedConfigId = e.ConfigId;

        var fromRadio = new FromRadio
        {
            Id = 4,
            ConfigCompleteId = 12345
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedConfigId.Should().Be(12345);
    }

    [Fact]
    public void Dispatch_Channel_RaisesChannelReceivedEvent()
    {
        // Arrange
        Channel? receivedChannel = null;
        _dispatcher.ChannelReceived += (sender, e) => receivedChannel = e.Channel;

        var channel = new Channel
        {
            Index = 0,
            Role = Channel.Types.Role.Primary,
            Settings = new ChannelSettings { Name = "Primary" }
        };

        var fromRadio = new FromRadio
        {
            Id = 5,
            Channel = channel
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedChannel.Should().NotBeNull();
        receivedChannel!.Index.Should().Be(0);
        receivedChannel.Role.Should().Be(Channel.Types.Role.Primary);
        receivedChannel.Settings.Name.Should().Be("Primary");
    }

    [Fact]
    public void Dispatch_Config_RaisesConfigReceivedEvent()
    {
        // Arrange
        Config? receivedConfig = null;
        _dispatcher.ConfigReceived += (sender, e) => receivedConfig = e.Config;

        var config = new Config();

        var fromRadio = new FromRadio
        {
            Id = 6,
            Config = config
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedConfig.Should().NotBeNull();
    }

    [Fact]
    public void Dispatch_ModuleConfig_RaisesModuleConfigReceivedEvent()
    {
        // Arrange
        ModuleConfig? receivedModuleConfig = null;
        _dispatcher.ModuleConfigReceived += (sender, e) => receivedModuleConfig = e.ModuleConfig;

        var moduleConfig = new ModuleConfig();

        var fromRadio = new FromRadio
        {
            Id = 7,
            ModuleConfig = moduleConfig
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedModuleConfig.Should().NotBeNull();
    }

    [Fact]
    public void Dispatch_Metadata_RaisesMetadataReceivedEvent()
    {
        // Arrange
        DeviceMetadata? receivedMetadata = null;
        _dispatcher.MetadataReceived += (sender, e) => receivedMetadata = e.Metadata;

        var metadata = new DeviceMetadata
        {
            FirmwareVersion = "2.3.0",
            HwModel = HardwareModel.Tbeam
        };

        var fromRadio = new FromRadio
        {
            Id = 8,
            Metadata = metadata
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        receivedMetadata.Should().NotBeNull();
        receivedMetadata!.FirmwareVersion.Should().Be("2.3.0");
        receivedMetadata.HwModel.Should().Be(HardwareModel.Tbeam);
    }

    [Fact]
    public void Dispatch_Rebooted_RaisesDeviceRebootedEvent()
    {
        // Arrange
        bool rebootedEventRaised = false;
        _dispatcher.DeviceRebooted += (sender, e) => rebootedEventRaised = true;

        var fromRadio = new FromRadio
        {
            Id = 9,
            Rebooted = true
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        rebootedEventRaised.Should().BeTrue();
    }

    [Fact]
    public void Dispatch_RebootedFalse_DoesNotRaiseEvent()
    {
        // Arrange
        bool rebootedEventRaised = false;
        _dispatcher.DeviceRebooted += (sender, e) => rebootedEventRaised = true;

        var fromRadio = new FromRadio
        {
            Id = 10,
            Rebooted = false
        };

        // Act
        _dispatcher.Dispatch(fromRadio);

        // Assert
        rebootedEventRaised.Should().BeFalse();
    }

    #endregion

    #region No Handler Tests

    [Fact]
    public void Dispatch_NoHandlerSubscribed_DoesNotThrow()
    {
        // Arrange
        var fromRadio = new FromRadio
        {
            Id = 11,
            Packet = new MeshPacket { From = 1, To = 2 }
        };

        // Act
        var action = () => _dispatcher.Dispatch(fromRadio);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispatch_NoPayload_DoesNotThrow()
    {
        // Arrange
        var fromRadio = new FromRadio { Id = 12 };

        // Act
        var action = () => _dispatcher.Dispatch(fromRadio);

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region Event Args Tests

    [Fact]
    public void MeshPacketReceivedEventArgs_ContainsPacket()
    {
        // Arrange
        var packet = new MeshPacket { From = 123, To = 456 };
        var args = new MeshPacketReceivedEventArgs(packet);

        // Assert
        args.Packet.Should().BeSameAs(packet);
    }

    [Fact]
    public void NodeInfoReceivedEventArgs_ContainsNodeInfo()
    {
        // Arrange
        var nodeInfo = new NodeInfo { Num = 789 };
        var args = new NodeInfoReceivedEventArgs(nodeInfo);

        // Assert
        args.NodeInfo.Should().BeSameAs(nodeInfo);
    }

    [Fact]
    public void MyNodeInfoReceivedEventArgs_ContainsMyInfo()
    {
        // Arrange
        var myInfo = new MyNodeInfo { MyNodeNum = 111 };
        var args = new MyNodeInfoReceivedEventArgs(myInfo);

        // Assert
        args.MyInfo.Should().BeSameAs(myInfo);
    }

    [Fact]
    public void ConfigCompleteEventArgs_ContainsConfigId()
    {
        // Arrange
        var args = new ConfigCompleteEventArgs(54321);

        // Assert
        args.ConfigId.Should().Be(54321);
    }

    [Fact]
    public void ChannelReceivedEventArgs_ContainsChannel()
    {
        // Arrange
        var channel = new Channel { Index = 2 };
        var args = new ChannelReceivedEventArgs(channel);

        // Assert
        args.Channel.Should().BeSameAs(channel);
    }

    #endregion
}
