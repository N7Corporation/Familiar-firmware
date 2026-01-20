using FluentAssertions;
using Xunit;

namespace Familiar.Audio.Tests;

/// <summary>
/// Unit tests for AudioOptions configuration.
/// </summary>
public class AudioOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new AudioOptions();

        options.OutputDevice.Should().Be("default");
        options.InputDevice.Should().Be("default");
        options.SampleRate.Should().Be(48000);
        options.BufferSize.Should().Be(1024);
        options.Volume.Should().BeApproximately(0.8f, 0.001f);
        options.MicMode.Should().Be("vox");
        options.VoxThreshold.Should().BeApproximately(0.02f, 0.001f);
        options.VoxHoldMs.Should().Be(500);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        AudioOptions.SectionName.Should().Be("Familiar:Audio");
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Volume_AcceptsValidRange(float volume)
    {
        var options = new AudioOptions { Volume = volume };
        options.Volume.Should().Be(volume);
    }

    [Theory]
    [InlineData("vox")]
    [InlineData("ptt")]
    public void MicMode_AcceptsValidModes(string mode)
    {
        var options = new AudioOptions { MicMode = mode };
        options.MicMode.Should().Be(mode);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(16000)]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void SampleRate_AcceptsCommonRates(int sampleRate)
    {
        var options = new AudioOptions { SampleRate = sampleRate };
        options.SampleRate.Should().Be(sampleRate);
    }

    [Theory]
    [InlineData(0.001f)]
    [InlineData(0.02f)]
    [InlineData(0.1f)]
    [InlineData(0.5f)]
    public void VoxThreshold_AcceptsValidRange(float threshold)
    {
        var options = new AudioOptions { VoxThreshold = threshold };
        options.VoxThreshold.Should().BeApproximately(threshold, 0.0001f);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    public void VoxHoldMs_AcceptsValidValues(int holdMs)
    {
        var options = new AudioOptions { VoxHoldMs = holdMs };
        options.VoxHoldMs.Should().Be(holdMs);
    }

    [Fact]
    public void BufferSize_CanBeModified()
    {
        var options = new AudioOptions { BufferSize = 4096 };
        options.BufferSize.Should().Be(4096);
    }

    [Fact]
    public void DeviceNames_CanBeCustomized()
    {
        var options = new AudioOptions
        {
            OutputDevice = "hw:0,0",
            InputDevice = "hw:1,0"
        };

        options.OutputDevice.Should().Be("hw:0,0");
        options.InputDevice.Should().Be("hw:1,0");
    }
}
