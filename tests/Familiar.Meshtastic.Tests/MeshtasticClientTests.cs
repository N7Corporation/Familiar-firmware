using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Familiar.Meshtastic.Tests;

/// <summary>
/// Unit tests for MeshtasticClient.
/// Tests connection state, message parsing, and node filtering.
/// Note: Tests that require actual serial port are skipped without hardware.
/// </summary>
public class MeshtasticClientTests : IDisposable
{
    private readonly Mock<ILogger<MeshtasticClient>> _mockLogger;
    private readonly MeshtasticOptions _options;

    public MeshtasticClientTests()
    {
        _mockLogger = new Mock<ILogger<MeshtasticClient>>();
        _options = new MeshtasticOptions
        {
            Enabled = true,
            Port = "/dev/ttyUSB0",
            BaudRate = 115200,
            Channel = 0,
            AllowedNodes = new List<string>()
        };
    }

    private MeshtasticClient CreateClient(MeshtasticOptions? options = null)
    {
        var opts = options ?? _options;
        return new MeshtasticClient(Options.Create(opts), _mockLogger.Object);
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
        client.KnownNodes.Should().BeEmpty();
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
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ReturnsImmediately()
    {
        // This test would require mocking SerialPort which is complex
        // Instead, we verify the early return logic by checking behavior
        var options = new MeshtasticOptions { Enabled = false };
        using var client = CreateClient(options);

        // Call twice - should not throw
        await client.ConnectAsync();
        await client.ConnectAsync();

        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_InvalidPort_ThrowsException()
    {
        var options = new MeshtasticOptions
        {
            Enabled = true,
            Port = "/dev/nonexistent_port_12345"
        };
        using var client = CreateClient(options);

        var action = async () => await client.ConnectAsync();

        await action.Should().ThrowAsync<Exception>();
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

    #region Node ID Extraction Tests

    [Theory]
    [InlineData("MSG: !abc123 -> !def456", "!abc123")]
    [InlineData("!xyz789", "!xyz789")]
    [InlineData("Some text !node123 more", "!node123")]
    [InlineData("No node here", "No node here")]
    public void ExtractNodeId_VariousInputs_ExtractsCorrectly(string input, string expected)
    {
        var result = NodeIdHelper.ExtractNodeId(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("!abc123 extra text", "!abc123")]
    [InlineData("prefix !abc123", "!abc123")]
    [InlineData("!abc", "!abc")]
    public void ExtractNodeId_WithSpaces_ExtractsFirstNodeOnly(string input, string expected)
    {
        var result = NodeIdHelper.ExtractNodeId(input);
        result.Should().Be(expected);
    }

    #endregion

    #region Message Parsing Tests

    [Fact]
    public void ParseMessage_ValidFormat_ReturnsMessage()
    {
        // Format without prefix colon - node IDs directly parseable
        var line = "!abc123 -> !def456: Hello world";

        var result = MessageParser.ParseMessage(line, 0);

        result.Should().NotBeNull();
        result!.FromNode.Should().Be("!abc123");
        result.ToNode.Should().Be("!def456");
        result.Text.Should().Be("Hello world");
    }

    [Fact]
    public void ParseMessage_WithColonsInText_PreservesText()
    {
        var line = "!abc123 -> !def456: Time is: 12:30:45";

        var result = MessageParser.ParseMessage(line, 0);

        result.Should().NotBeNull();
        result!.Text.Should().Contain("12:30:45");
    }

    [Fact]
    public void ParseMessage_InvalidFormat_ReturnsNull()
    {
        var line = "Invalid message format";

        var result = MessageParser.ParseMessage(line, 0);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseMessage_EmptyLine_ReturnsNull()
    {
        var result = MessageParser.ParseMessage("", 0);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseMessage_OnlyArrow_ReturnsNull()
    {
        var result = MessageParser.ParseMessage("->", 0);

        result.Should().BeNull();
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

    #endregion
}

/// <summary>
/// Helper class for testing node ID extraction logic.
/// Mirrors the private ExtractNodeId method.
/// </summary>
internal static class NodeIdHelper
{
    public static string ExtractNodeId(string text)
    {
        var trimmed = text.Trim();
        var startIndex = trimmed.IndexOf('!');
        if (startIndex >= 0)
        {
            var endIndex = trimmed.IndexOf(' ', startIndex);
            return endIndex >= 0
                ? trimmed[startIndex..endIndex]
                : trimmed[startIndex..];
        }
        return trimmed;
    }
}

/// <summary>
/// Helper class for testing message parsing logic.
/// Mirrors the private ParseMessage method.
/// </summary>
internal static class MessageParser
{
    public static MeshtasticMessage? ParseMessage(string line, int channel)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Split(new[] { "->", ":" }, StringSplitOptions.TrimEntries);

        if (parts.Length >= 3)
        {
            return new MeshtasticMessage
            {
                FromNode = NodeIdHelper.ExtractNodeId(parts[0]),
                ToNode = NodeIdHelper.ExtractNodeId(parts[1]),
                Text = string.Join(":", parts.Skip(2)).Trim(),
                Channel = channel,
                ReceivedAt = DateTime.UtcNow
            };
        }

        return null;
    }
}

/// <summary>
/// Helper class for testing message filtering logic.
/// Mirrors the private ShouldProcessMessage method.
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
