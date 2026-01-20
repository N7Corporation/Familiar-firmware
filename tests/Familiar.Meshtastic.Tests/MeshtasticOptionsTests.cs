using FluentAssertions;
using Xunit;

namespace Familiar.Meshtastic.Tests;

/// <summary>
/// Unit tests for MeshtasticOptions configuration.
/// </summary>
public class MeshtasticOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new MeshtasticOptions();

        options.Enabled.Should().BeTrue();
        options.Port.Should().Be("/dev/ttyUSB0");
        options.BaudRate.Should().Be(115200);
        options.NodeName.Should().Be("Familiar");
        options.Channel.Should().Be(0);
        options.AllowedNodes.Should().BeEmpty();
        options.ReconnectDelaySeconds.Should().Be(5);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        MeshtasticOptions.SectionName.Should().Be("Familiar:Meshtastic");
    }

    [Theory]
    [InlineData("/dev/ttyUSB0")]
    [InlineData("/dev/ttyUSB1")]
    [InlineData("/dev/ttyACM0")]
    [InlineData("COM3")]
    public void Port_AcceptsValidPorts(string port)
    {
        var options = new MeshtasticOptions { Port = port };
        options.Port.Should().Be(port);
    }

    [Theory]
    [InlineData(9600)]
    [InlineData(19200)]
    [InlineData(38400)]
    [InlineData(57600)]
    [InlineData(115200)]
    [InlineData(230400)]
    [InlineData(460800)]
    [InlineData(921600)]
    public void BaudRate_AcceptsCommonRates(int baudRate)
    {
        var options = new MeshtasticOptions { BaudRate = baudRate };
        options.BaudRate.Should().Be(baudRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Channel_AcceptsValidChannels(int channel)
    {
        var options = new MeshtasticOptions { Channel = channel };
        options.Channel.Should().Be(channel);
    }

    [Fact]
    public void AllowedNodes_CanBePopulated()
    {
        var options = new MeshtasticOptions
        {
            AllowedNodes = new List<string> { "!abc123", "!def456", "!ghi789" }
        };

        options.AllowedNodes.Should().HaveCount(3);
        options.AllowedNodes.Should().Contain("!abc123");
        options.AllowedNodes.Should().Contain("!def456");
        options.AllowedNodes.Should().Contain("!ghi789");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    public void ReconnectDelaySeconds_AcceptsValidValues(int delay)
    {
        var options = new MeshtasticOptions { ReconnectDelaySeconds = delay };
        options.ReconnectDelaySeconds.Should().Be(delay);
    }

    [Fact]
    public void NodeName_CanBeCustomized()
    {
        var options = new MeshtasticOptions { NodeName = "MyDevice" };
        options.NodeName.Should().Be("MyDevice");
    }

    [Fact]
    public void Enabled_CanBeDisabled()
    {
        var options = new MeshtasticOptions { Enabled = false };
        options.Enabled.Should().BeFalse();
    }
}
