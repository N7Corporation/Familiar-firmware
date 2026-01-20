using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Familiar.Camera.Tests;

/// <summary>
/// Unit tests for CameraService.
/// Tests state management, filename sanitization, and recording info.
/// Note: Tests requiring actual camera hardware are marked with [Trait].
/// </summary>
public class CameraServiceTests : IDisposable
{
    private readonly Mock<ILogger<CameraService>> _mockLogger;
    private readonly CameraOptions _options;
    private readonly string _testRecordingPath;

    public CameraServiceTests()
    {
        _mockLogger = new Mock<ILogger<CameraService>>();
        _testRecordingPath = Path.Combine(Path.GetTempPath(), $"familiar_camera_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRecordingPath);

        _options = new CameraOptions
        {
            Enabled = true,
            Width = 1920,
            Height = 1080,
            Framerate = 30,
            RecordingPath = _testRecordingPath,
            StreamBitrate = 4_000_000,
            RecordingBitrate = 8_000_000,
            SnapshotQuality = 90
        };
    }

    private CameraService CreateService(CameraOptions? options = null)
    {
        var opts = options ?? _options;
        return new CameraService(Options.Create(opts), _mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup test directory
        try
        {
            if (Directory.Exists(_testRecordingPath))
            {
                Directory.Delete(_testRecordingPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitialState_NotStreamingOrRecording()
    {
        using var service = CreateService();

        service.IsStreaming.Should().BeFalse();
        service.IsRecording.Should().BeFalse();
        service.CurrentRecordingFile.Should().BeNull();
    }

    [Fact]
    public void Constructor_DisabledCamera_NotAvailable()
    {
        var options = new CameraOptions { Enabled = false };
        using var service = CreateService(options);

        service.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_CreatesRecordingDirectory()
    {
        var newPath = Path.Combine(_testRecordingPath, "subdir");
        var options = new CameraOptions
        {
            Enabled = true,
            RecordingPath = newPath
        };

        using var service = CreateService(options);

        Directory.Exists(newPath).Should().BeTrue();
    }

    #endregion

    #region Recording List Tests

    [Fact]
    public async Task GetRecordingsAsync_EmptyDirectory_ReturnsEmptyList()
    {
        using var service = CreateService();

        var recordings = await service.GetRecordingsAsync();

        recordings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecordingsAsync_WithH264Files_ReturnsRecordings()
    {
        // Create test files
        var file1 = Path.Combine(_testRecordingPath, "test1.h264");
        var file2 = Path.Combine(_testRecordingPath, "test2.h264");
        await File.WriteAllBytesAsync(file1, new byte[] { 0x00, 0x00, 0x00, 0x01 });
        await Task.Delay(50); // Ensure different timestamps
        await File.WriteAllBytesAsync(file2, new byte[] { 0x00, 0x00, 0x00, 0x01, 0x67 });

        using var service = CreateService();

        var recordings = await service.GetRecordingsAsync();

        recordings.Should().HaveCount(2);
        recordings.Should().Contain(r => r.Filename == "test1.h264");
        recordings.Should().Contain(r => r.Filename == "test2.h264");
    }

    [Fact]
    public async Task GetRecordingsAsync_WithMp4Files_ReturnsRecordings()
    {
        var file = Path.Combine(_testRecordingPath, "test.mp4");
        await File.WriteAllBytesAsync(file, new byte[] { 0x00, 0x00, 0x00, 0x18 });

        using var service = CreateService();

        var recordings = await service.GetRecordingsAsync();

        recordings.Should().ContainSingle(r => r.Filename == "test.mp4");
    }

    [Fact]
    public async Task GetRecordingsAsync_SortsByCreationTimeDescending()
    {
        var file1 = Path.Combine(_testRecordingPath, "older.h264");
        await File.WriteAllBytesAsync(file1, new byte[] { 0x01 });
        await Task.Delay(100);
        var file2 = Path.Combine(_testRecordingPath, "newer.h264");
        await File.WriteAllBytesAsync(file2, new byte[] { 0x02 });

        using var service = CreateService();

        var recordings = await service.GetRecordingsAsync();

        recordings.Should().HaveCount(2);
        recordings[0].Filename.Should().Be("newer.h264");
        recordings[1].Filename.Should().Be("older.h264");
    }

    [Fact]
    public async Task GetRecordingsAsync_ReturnsCorrectFileInfo()
    {
        var content = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x67, 0x42, 0x00, 0x1e };
        var file = Path.Combine(_testRecordingPath, "info_test.h264");
        await File.WriteAllBytesAsync(file, content);

        using var service = CreateService();

        var recordings = await service.GetRecordingsAsync();

        recordings.Should().ContainSingle();
        var recording = recordings[0];
        recording.Filename.Should().Be("info_test.h264");
        recording.FullPath.Should().Be(file);
        recording.SizeBytes.Should().Be(content.Length);
        recording.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Delete Recording Tests

    [Fact]
    public async Task DeleteRecordingAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        var file = Path.Combine(_testRecordingPath, "to_delete.h264");
        await File.WriteAllBytesAsync(file, new byte[] { 0x01 });

        using var service = CreateService();

        var result = await service.DeleteRecordingAsync("to_delete.h264");

        result.Should().BeTrue();
        File.Exists(file).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRecordingAsync_NonExistentFile_ReturnsFalse()
    {
        using var service = CreateService();

        var result = await service.DeleteRecordingAsync("nonexistent.h264");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\important.txt")]
    [InlineData("/etc/passwd")]
    public async Task DeleteRecordingAsync_PathTraversal_OnlyUsesFilename(string maliciousPath)
    {
        // Create a file with just the filename portion
        var safeName = Path.GetFileName(maliciousPath);
        var file = Path.Combine(_testRecordingPath, safeName);

        // This should NOT delete files outside the recording directory
        using var service = CreateService();

        var result = await service.DeleteRecordingAsync(maliciousPath);

        // Should return false since the sanitized filename doesn't exist
        result.Should().BeFalse();
    }

    #endregion

    #region Filename Sanitization Tests

    [Theory]
    [InlineData("normal_filename", "normal_filename")]
    [InlineData("test/file", "test_file")]  // Forward slash is invalid on all platforms
    public void SanitizeFilename_RemovesInvalidCharacters(string input, string expected)
    {
        var result = FilenameHelper.Sanitize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeFilename_RemovesAllInvalidChars()
    {
        // Get the actual invalid chars for this platform
        var invalidChars = Path.GetInvalidFileNameChars();

        // Build a test string with all invalid chars
        var input = "test" + new string(invalidChars) + "file";
        var result = FilenameHelper.Sanitize(input);

        // Result should not contain any invalid characters
        result.Should().NotContainAny(invalidChars.Select(c => c.ToString()).ToArray());
    }

    #endregion

    #region Streaming Tests (State Only)

    [Fact]
    public async Task StartStreamingAsync_WhenDisabled_ReturnsFalse()
    {
        var options = new CameraOptions { Enabled = false };
        using var service = CreateService(options);

        var result = await service.StartStreamingAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task StopStreamingAsync_WhenNotStreaming_DoesNotThrow()
    {
        using var service = CreateService();

        var action = async () => await service.StopStreamingAsync();

        await action.Should().NotThrowAsync();
    }

    #endregion

    #region Recording Tests (State Only)

    [Fact]
    public async Task StartRecordingAsync_WhenDisabled_ReturnsFalse()
    {
        var options = new CameraOptions { Enabled = false };
        using var service = CreateService(options);

        var result = await service.StartRecordingAsync("test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task StopRecordingAsync_WhenNotRecording_ReturnsNull()
    {
        using var service = CreateService();

        var result = await service.StopRecordingAsync();

        result.Should().BeNull();
    }

    #endregion

    #region Snapshot Tests (State Only)

    [Fact]
    public async Task CaptureSnapshotAsync_WhenDisabled_ReturnsNull()
    {
        var options = new CameraOptions { Enabled = false };
        using var service = CreateService(options);

        var result = await service.CaptureSnapshotAsync();

        result.Should().BeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var service = CreateService();

        var action = () =>
        {
            service.Dispose();
            service.Dispose();
            service.Dispose();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenNotStreaming_DoesNotThrow()
    {
        var service = CreateService();

        var action = () => service.Dispose();

        action.Should().NotThrow();
    }

    #endregion
}

/// <summary>
/// Helper class for testing filename sanitization logic.
/// Mirrors the private SanitizeFilename method.
/// </summary>
internal static class FilenameHelper
{
    public static string Sanitize(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Tests for RecordingInfo record.
/// </summary>
public class RecordingInfoTests
{
    [Fact]
    public void RecordingInfo_RequiredProperties_MustBeSet()
    {
        var info = new RecordingInfo
        {
            Filename = "test.h264",
            FullPath = "/path/to/test.h264",
            SizeBytes = 1024,
            CreatedAt = DateTime.UtcNow
        };

        info.Filename.Should().Be("test.h264");
        info.FullPath.Should().Be("/path/to/test.h264");
        info.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void RecordingInfo_DefaultSizeBytes_IsZero()
    {
        var info = new RecordingInfo
        {
            Filename = "test.h264",
            FullPath = "/path/to/test.h264"
        };

        info.SizeBytes.Should().Be(0);
    }

    [Fact]
    public void RecordingInfo_RecordEquality_Works()
    {
        var time = DateTime.UtcNow;
        var info1 = new RecordingInfo
        {
            Filename = "test.h264",
            FullPath = "/path/test.h264",
            SizeBytes = 1024,
            CreatedAt = time
        };
        var info2 = new RecordingInfo
        {
            Filename = "test.h264",
            FullPath = "/path/test.h264",
            SizeBytes = 1024,
            CreatedAt = time
        };

        info1.Should().Be(info2);
    }
}
