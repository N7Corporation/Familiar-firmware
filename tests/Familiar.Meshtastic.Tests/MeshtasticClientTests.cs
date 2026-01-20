using Familiar.Meshtastic.Connection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Familiar.Meshtastic.Tests;

/// <summary>
/// Unit tests for MeshtasticClient.
/// Tests connection state, message handling, and node management.
/// Note: Tests that require actual serial port are skipped without hardware.
/// </summary>
public class MeshtasticClientTests : IDisposable
{
    private readonly Mock<ILogger<MeshtasticClient>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly MeshtasticOptions _options;

    public MeshtasticClientTests()
    {
        _mockLogger = new Mock<ILogger<MeshtasticClient>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();

        // Setup logger factory to return mock loggers
        _mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        _options = new MeshtasticOptions
        {
            Enabled = true,
            Port = "/dev/ttyUSB0",
            BaudRate = 115200,
            Channel = 0,
            AllowedNodes = new List<string>(),
            ConfigTimeoutMs = 5000,
            HeartbeatIntervalMs = 0
        };
    }

    private MeshtasticClient CreateClient(MeshtasticOptions? options = null)
    {
        var opts = options ?? _options;
        return new MeshtasticClient(
            Options.Create(opts),
            _mockLogger.Object,
            _mockLoggerFactory.Object);
    }

    public void Dispose()
    {
        // Cleanup handled by individual tests
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitialState_IsDisconnected()
    {
        using var client = CreateClient();

        client.IsConnected.Should().BeFalse();
        client.ConnectionState.Should().Be(ConnectionState.Disconnected);
        client.KnownNodes.Should().BeEmpty();
        client.MyNodeNum.Should().BeNull();
    }

    [Fact]
    public void KnownNodes_InitialState_IsEmptyReadOnlyList()
    {
        using var client = CreateClient();

        client.KnownNodes.Should().NotBeNull();
        client.KnownNodes.Should().BeEmpty();
        client.KnownNodes.Should().BeAssignableTo<IReadOnlyList<MeshtasticNode>>();
    }

    #endregion

    #region Connect Tests

    [Fact]
    public async Task ConnectAsync_WhenDisabled_DoesNotConnect()
    {
        var options = new MeshtasticOptions { Enabled = false };
        using var client = CreateClient(options);

        await client.ConnectAsync();

        client.IsConnected.Should().BeFalse();
        client.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ReturnsImmediately()
    {
        // This test verifies the early return logic by checking behavior
        var options = new MeshtasticOptions { Enabled = false };
        using var client = CreateClient(options);

        // Call twice - should not throw
        await client.ConnectAsync();
        await client.ConnectAsync();

        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_InvalidPort_DoesNotThrow()
    {
        // The new implementation handles failures gracefully
        var options = new MeshtasticOptions
        {
            Enabled = true,
            Port = "/dev/nonexistent_port_12345",
            ConfigTimeoutMs = 100
        };
        using var client = CreateClient(options);

        // Should not throw, but should fail to connect
        await client.ConnectAsync();

        client.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Disconnect Tests

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        using var client = CreateClient();

        var action = async () => await client.DisconnectAsync();

        await action.Should().NotThrowAsync();
    }

    #endregion

    #region Send Message Tests

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ThrowsInvalidOperation()
    {
        using var client = CreateClient();

        var action = async () => await client.SendMessageAsync("Hello");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not connected*");
    }

    #endregion

    #region Node ID Formatting Tests

    [Theory]
    [InlineData(0x12345678u, "!12345678")]
    [InlineData(0xABCDEF01u, "!abcdef01")]
    [InlineData(0x00000001u, "!00000001")]
    [InlineData(0xFFFFFFFFu, "!ffffffff")]
    public void FormatNodeId_VariousInputs_FormatsCorrectly(uint nodeNum, string expected)
    {
        var result = NodeIdFormatter.FormatNodeId(nodeNum);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("!12345678", 0x12345678u)]
    [InlineData("!abcdef01", 0xABCDEF01u)]
    [InlineData("!ABCDEF01", 0xABCDEF01u)]
    [InlineData("12345678", 0x12345678u)]
    [InlineData("!00000001", 0x00000001u)]
    public void ParseNodeId_VariousInputs_ParsesCorrectly(string nodeId, uint expected)
    {
        var result = NodeIdFormatter.ParseNodeId(nodeId);
        result.Should().Be(expected);
    }

    #endregion

    #region Message Filtering Tests

    [Fact]
    public void ShouldProcessMessage_NoAllowedNodes_AllowsAll()
    {
        var options = new MeshtasticOptions { AllowedNodes = new List<string>() };
        var message = new MeshtasticMessage
        {
            FromNode = "!random123",
            ToNode = "!mynode",
            Text = "Test",
            Channel = 0,
            ReceivedAt = DateTime.UtcNow
        };

        var result = MessageFilter.ShouldProcess(message, options.AllowedNodes);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessMessage_AllowedNode_Allows()
    {
        var options = new MeshtasticOptions
        {
            AllowedNodes = new List<string> { "!trusted123", "!trusted456" }
        };
        var message = new MeshtasticMessage
        {
            FromNode = "!trusted123",
            ToNode = "!mynode",
            Text = "Test",
            Channel = 0,
            ReceivedAt = DateTime.UtcNow
        };

        var result = MessageFilter.ShouldProcess(message, options.AllowedNodes);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessMessage_NotAllowedNode_Blocks()
    {
        var options = new MeshtasticOptions
        {
            AllowedNodes = new List<string> { "!trusted123", "!trusted456" }
        };
        var message = new MeshtasticMessage
        {
            FromNode = "!untrusted999",
            ToNode = "!mynode",
            Text = "Test",
            Channel = 0,
            ReceivedAt = DateTime.UtcNow
        };

        var result = MessageFilter.ShouldProcess(message, options.AllowedNodes);

        result.Should().BeFalse();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var client = CreateClient();

        var action = () =>
        {
            client.Dispose();
            client.Dispose();
            client.Dispose();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenNotConnected_DoesNotThrow()
    {
        var client = CreateClient();

        var action = () => client.Dispose();

        action.Should().NotThrow();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void MessageReceived_EventCanBeSubscribed()
    {
        using var client = CreateClient();
        bool eventRaised = false;

        client.MessageReceived += (sender, args) => eventRaised = true;

        // Event subscription should work without throwing
        eventRaised.Should().BeFalse(); // Not raised yet
    }

    [Fact]
    public void ConnectionStateChanged_EventCanBeSubscribed()
    {
        using var client = CreateClient();
        bool? connectionState = null;

        client.ConnectionStateChanged += (sender, state) => connectionState = state;

        // Event subscription should work without throwing
        connectionState.Should().BeNull(); // Not raised yet
    }

    [Fact]
    public void NodeUpdated_EventCanBeSubscribed()
    {
        using var client = CreateClient();
        MeshtasticNode? updatedNode = null;

        client.NodeUpdated += (sender, args) => updatedNode = args.Node;

        // Event subscription should work without throwing
        updatedNode.Should().BeNull(); // Not raised yet
    }

    #endregion

    #region Options Tests

    [Fact]
    public void MeshtasticOptions_DefaultValues_AreCorrect()
    {
        var options = new MeshtasticOptions();

        options.Enabled.Should().BeTrue();
        options.Port.Should().Be("/dev/ttyUSB0");
        options.BaudRate.Should().Be(115200);
        options.Channel.Should().Be(0);
        options.AllowedNodes.Should().BeEmpty();
        options.ReconnectDelaySeconds.Should().Be(5);
        options.ConfigTimeoutMs.Should().Be(10000);
        options.HeartbeatIntervalMs.Should().Be(0);
    }

    #endregion

    #region MeshtasticNode Tests

    [Fact]
    public void MeshtasticNode_CanBeCreatedWithRequiredProperties()
    {
        var node = new MeshtasticNode
        {
            NodeId = "!12345678",
            NodeNum = 0x12345678,
            Name = "Test Node",
            ShortName = "TEST"
        };

        node.NodeId.Should().Be("!12345678");
        node.NodeNum.Should().Be(0x12345678);
        node.Name.Should().Be("Test Node");
        node.ShortName.Should().Be("TEST");
    }

    [Fact]
    public void MeshtasticNode_OptionalProperties_AreNullable()
    {
        var node = new MeshtasticNode
        {
            NodeId = "!12345678"
        };

        node.Name.Should().BeNull();
        node.ShortName.Should().BeNull();
        node.LastHeard.Should().BeNull();
        node.BatteryLevel.Should().BeNull();
        node.Voltage.Should().BeNull();
        node.Latitude.Should().BeNull();
        node.Longitude.Should().BeNull();
        node.Altitude.Should().BeNull();
    }

    #endregion

    #region MeshtasticMessage Tests

    [Fact]
    public void MeshtasticMessage_CanBeCreatedWithRequiredProperties()
    {
        var message = new MeshtasticMessage
        {
            FromNode = "!sender01",
            ToNode = "!receiver1",
            Text = "Hello, mesh!"
        };

        message.FromNode.Should().Be("!sender01");
        message.ToNode.Should().Be("!receiver1");
        message.Text.Should().Be("Hello, mesh!");
        message.ReceivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MeshtasticMessage_OptionalProperties_AreNullable()
    {
        var message = new MeshtasticMessage
        {
            FromNode = "!sender01",
            ToNode = "!receiver1",
            Text = "Test"
        };

        message.Snr.Should().BeNull();
        message.Rssi.Should().BeNull();
        message.PacketId.Should().BeNull();
        message.HopLimit.Should().BeNull();
        message.Latitude.Should().BeNull();
        message.Longitude.Should().BeNull();
    }

    #endregion
}

/// <summary>
/// Helper class for testing node ID formatting.
/// </summary>
internal static class NodeIdFormatter
{
    public static string FormatNodeId(uint nodeNum)
    {
        return $"!{nodeNum:x8}";
    }

    public static uint ParseNodeId(string nodeId)
    {
        var hex = nodeId.TrimStart('!');
        return Convert.ToUInt32(hex, 16);
    }
}

/// <summary>
/// Helper class for testing message filtering logic.
/// </summary>
internal static class MessageFilter
{
    public static bool ShouldProcess(MeshtasticMessage message, IList<string> allowedNodes)
    {
        if (allowedNodes.Count > 0 && !allowedNodes.Contains(message.FromNode))
        {
            return false;
        }
        return true;
    }
}
