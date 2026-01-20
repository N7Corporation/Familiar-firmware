using FluentAssertions;
using Xunit;

namespace Familiar.Camera.Tests;

/// <summary>
/// Unit tests for CameraOptions configuration.
/// </summary>
public class CameraOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CameraOptions();

        options.Enabled.Should().BeFalse("Camera should be disabled by default");
        options.Width.Should().Be(1920);
        options.Height.Should().Be(1080);
        options.Framerate.Should().Be(30);
        options.RecordingPath.Should().Be("/home/familiar/recordings");
        options.StreamBitrate.Should().Be(4_000_000);
        options.RecordingBitrate.Should().Be(8_000_000);
        options.SnapshotQuality.Should().Be(90);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        CameraOptions.SectionName.Should().Be("Familiar:Camera");
    }

    [Theory]
    [InlineData(640, 480)]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    public void Resolution_AcceptsCommonValues(int width, int height)
    {
        var options = new CameraOptions { Width = width, Height = height };

        options.Width.Should().Be(width);
        options.Height.Should().Be(height);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(24)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void Framerate_AcceptsCommonValues(int framerate)
    {
        var options = new CameraOptions { Framerate = framerate };
        options.Framerate.Should().Be(framerate);
    }

    [Theory]
    [InlineData(1_000_000)]   // 1 Mbps
    [InlineData(2_000_000)]   // 2 Mbps
    [InlineData(4_000_000)]   // 4 Mbps
    [InlineData(8_000_000)]   // 8 Mbps
    [InlineData(16_000_000)]  // 16 Mbps
    public void StreamBitrate_AcceptsValidValues(int bitrate)
    {
        var options = new CameraOptions { StreamBitrate = bitrate };
        options.StreamBitrate.Should().Be(bitrate);
    }

    [Theory]
    [InlineData(4_000_000)]   // 4 Mbps
    [InlineData(8_000_000)]   // 8 Mbps
    [InlineData(16_000_000)]  // 16 Mbps
    [InlineData(25_000_000)]  // 25 Mbps
    public void RecordingBitrate_AcceptsValidValues(int bitrate)
    {
        var options = new CameraOptions { RecordingBitrate = bitrate };
        options.RecordingBitrate.Should().Be(bitrate);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(90)]
    [InlineData(95)]
    [InlineData(100)]
    public void SnapshotQuality_AcceptsValidRange(int quality)
    {
        var options = new CameraOptions { SnapshotQuality = quality };
        options.SnapshotQuality.Should().Be(quality);
    }

    [Theory]
    [InlineData("/home/familiar/recordings")]
    [InlineData("/tmp/recordings")]
    [InlineData("/var/lib/familiar/videos")]
    public void RecordingPath_AcceptsValidPaths(string path)
    {
        var options = new CameraOptions { RecordingPath = path };
        options.RecordingPath.Should().Be(path);
    }

    [Fact]
    public void Enabled_CanBeEnabled()
    {
        var options = new CameraOptions { Enabled = true };
        options.Enabled.Should().BeTrue();
    }
}
