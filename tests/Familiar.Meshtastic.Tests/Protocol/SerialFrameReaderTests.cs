using Familiar.Meshtastic.Protocol;
using FluentAssertions;
using Xunit;

namespace Familiar.Meshtastic.Tests.Protocol;

/// <summary>
/// Unit tests for SerialFrameReader.
/// </summary>
public class SerialFrameReaderTests
{
    #region Frame Reading Tests

    [Fact]
    public async Task ReadFrameAsync_ValidFrame_ReturnsPayload()
    {
        // Arrange
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var frame = CreateFrame(payload);
        using var stream = new MemoryStream(frame);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(payload);
    }

    [Fact]
    public async Task ReadFrameAsync_EmptyStream_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream(Array.Empty<byte>());
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_OnlyMagicBytes_ReturnsNull()
    {
        // Arrange
        var data = new byte[] { FrameConstants.Magic1, FrameConstants.Magic2 };
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_GarbageBeforeFrame_SyncsAndReadsFrame()
    {
        // Arrange
        var payload = new byte[] { 0xAA, 0xBB };
        var garbage = new byte[] { 0xFF, 0xFE, 0xFD, 0x00, 0x01 };
        var frame = CreateFrame(payload);
        var data = garbage.Concat(frame).ToArray();
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(payload);
    }

    [Fact]
    public async Task ReadFrameAsync_MultipleFrames_ReadsFirstFrame()
    {
        // Arrange
        var payload1 = new byte[] { 0x01, 0x02 };
        var payload2 = new byte[] { 0x03, 0x04, 0x05 };
        var frame1 = CreateFrame(payload1);
        var frame2 = CreateFrame(payload2);
        var data = frame1.Concat(frame2).ToArray();
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(payload1);
    }

    [Fact]
    public async Task ReadFrameAsync_MultipleFrames_ReadsAllSequentially()
    {
        // Arrange
        var payload1 = new byte[] { 0x01, 0x02 };
        var payload2 = new byte[] { 0x03, 0x04, 0x05 };
        var frame1 = CreateFrame(payload1);
        var frame2 = CreateFrame(payload2);
        var data = frame1.Concat(frame2).ToArray();
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result1 = await reader.ReadFrameAsync();
        var result2 = await reader.ReadFrameAsync();

        // Assert
        result1.Should().Equal(payload1);
        result2.Should().Equal(payload2);
    }

    [Fact]
    public async Task ReadFrameAsync_InvalidLength_ReturnsNull()
    {
        // Arrange - length says 1000 bytes but only 2 in payload
        var data = new byte[]
        {
            FrameConstants.Magic1, FrameConstants.Magic2,
            0x03, 0xE8, // Length = 1000 (exceeds max)
            0x01, 0x02
        };
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_ZeroLength_ReturnsNull()
    {
        // Arrange
        var data = new byte[]
        {
            FrameConstants.Magic1, FrameConstants.Magic2,
            0x00, 0x00 // Length = 0
        };
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_TruncatedPayload_ReturnsNull()
    {
        // Arrange - header says 10 bytes but only 3 available
        var data = new byte[]
        {
            FrameConstants.Magic1, FrameConstants.Magic2,
            0x00, 0x0A, // Length = 10
            0x01, 0x02, 0x03 // Only 3 bytes
        };
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_MaxPayloadSize_ReadsSuccessfully()
    {
        // Arrange
        var payload = new byte[FrameConstants.MaxPayloadSize];
        new Random(42).NextBytes(payload);
        var frame = CreateFrame(payload);
        using var stream = new MemoryStream(frame);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(payload);
    }

    [Fact]
    public async Task ReadFrameAsync_CancellationRequested_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[100]);
        var reader = new SerialFrameReader(stream);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await reader.ReadFrameAsync(cts.Token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadFrameAsync_FalseMagic1InData_ContinuesSearching()
    {
        // Arrange - Magic1 byte in garbage followed by real frame
        var payload = new byte[] { 0xAA };
        var garbage = new byte[] { FrameConstants.Magic1, 0x00, 0x00 }; // Magic1 but not Magic2
        var frame = CreateFrame(payload);
        var data = garbage.Concat(frame).ToArray();
        using var stream = new MemoryStream(data);
        var reader = new SerialFrameReader(stream);

        // Act
        var result = await reader.ReadFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(payload);
    }

    #endregion

    #region Frame Constants Tests

    [Fact]
    public void FrameConstants_MagicBytes_AreCorrect()
    {
        FrameConstants.Magic1.Should().Be(0x94);
        FrameConstants.Magic2.Should().Be(0xC3);
    }

    [Fact]
    public void FrameConstants_HeaderSize_Is4Bytes()
    {
        FrameConstants.HeaderSize.Should().Be(4);
    }

    [Fact]
    public void FrameConstants_MaxPayloadSize_Is512()
    {
        FrameConstants.MaxPayloadSize.Should().Be(512);
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateFrame(byte[] payload)
    {
        var frame = new byte[FrameConstants.HeaderSize + payload.Length];
        frame[0] = FrameConstants.Magic1;
        frame[1] = FrameConstants.Magic2;
        frame[2] = (byte)(payload.Length >> 8);   // MSB
        frame[3] = (byte)(payload.Length & 0xFF); // LSB
        Array.Copy(payload, 0, frame, FrameConstants.HeaderSize, payload.Length);
        return frame;
    }

    #endregion
}
