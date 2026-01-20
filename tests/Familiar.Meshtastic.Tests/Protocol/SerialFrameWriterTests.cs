using Familiar.Meshtastic.Protocol;
using FluentAssertions;
using Xunit;

namespace Familiar.Meshtastic.Tests.Protocol;

/// <summary>
/// Unit tests for SerialFrameWriter.
/// </summary>
public class SerialFrameWriterTests
{
    #region Write Frame Tests

    [Fact]
    public async Task WriteFrameAsync_ValidPayload_WritesCorrectFrame()
    {
        // Arrange
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(payload);

        // Assert
        var written = stream.ToArray();
        written.Length.Should().Be(FrameConstants.HeaderSize + payload.Length);
        written[0].Should().Be(FrameConstants.Magic1);
        written[1].Should().Be(FrameConstants.Magic2);
        written[2].Should().Be(0x00); // Length MSB
        written[3].Should().Be(0x04); // Length LSB
        written.Skip(FrameConstants.HeaderSize).Should().Equal(payload);
    }

    [Fact]
    public async Task WriteFrameAsync_EmptyPayload_WritesFrameWithZeroLength()
    {
        // Arrange
        var payload = Array.Empty<byte>();
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(payload);

        // Assert
        var written = stream.ToArray();
        written.Length.Should().Be(FrameConstants.HeaderSize);
        written[0].Should().Be(FrameConstants.Magic1);
        written[1].Should().Be(FrameConstants.Magic2);
        written[2].Should().Be(0x00);
        written[3].Should().Be(0x00);
    }

    [Fact]
    public async Task WriteFrameAsync_LargePayload_WritesCorrectLength()
    {
        // Arrange
        var payload = new byte[300];
        new Random(42).NextBytes(payload);
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(payload);

        // Assert
        var written = stream.ToArray();
        written.Length.Should().Be(FrameConstants.HeaderSize + 300);
        written[2].Should().Be(0x01); // Length MSB (300 = 0x012C)
        written[3].Should().Be(0x2C); // Length LSB
    }

    [Fact]
    public async Task WriteFrameAsync_MaxPayload_WritesSuccessfully()
    {
        // Arrange
        var payload = new byte[FrameConstants.MaxPayloadSize];
        new Random(42).NextBytes(payload);
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(payload);

        // Assert
        var written = stream.ToArray();
        written.Length.Should().Be(FrameConstants.HeaderSize + FrameConstants.MaxPayloadSize);
        written.Skip(FrameConstants.HeaderSize).Should().Equal(payload);
    }

    [Fact]
    public async Task WriteFrameAsync_ExceedsMaxPayload_ThrowsArgumentException()
    {
        // Arrange
        var payload = new byte[FrameConstants.MaxPayloadSize + 1];
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        var action = async () => await writer.WriteFrameAsync(payload);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds maximum*");
    }

    [Fact]
    public async Task WriteFrameAsync_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        var action = async () => await writer.WriteFrameAsync((byte[])null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteFrameAsync_ReadOnlyMemory_WritesCorrectFrame()
    {
        // Arrange
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        ReadOnlyMemory<byte> memory = payload;
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(memory);

        // Assert
        var written = stream.ToArray();
        written.Length.Should().Be(FrameConstants.HeaderSize + payload.Length);
        written.Skip(FrameConstants.HeaderSize).Should().Equal(payload);
    }

    [Fact]
    public async Task WriteFrameAsync_MultipleWrites_AllSucceed()
    {
        // Arrange
        var payload1 = new byte[] { 0x01, 0x02 };
        var payload2 = new byte[] { 0x03, 0x04, 0x05 };
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);

        // Act
        await writer.WriteFrameAsync(payload1);
        await writer.WriteFrameAsync(payload2);

        // Assert
        var written = stream.ToArray();
        var expectedLength = (FrameConstants.HeaderSize + payload1.Length) +
                            (FrameConstants.HeaderSize + payload2.Length);
        written.Length.Should().Be(expectedLength);

        // Verify first frame
        written[0].Should().Be(FrameConstants.Magic1);
        written[1].Should().Be(FrameConstants.Magic2);
        written[2].Should().Be(0x00);
        written[3].Should().Be(0x02);

        // Verify second frame starts at correct offset
        var frame2Start = FrameConstants.HeaderSize + payload1.Length;
        written[frame2Start].Should().Be(FrameConstants.Magic1);
        written[frame2Start + 1].Should().Be(FrameConstants.Magic2);
        written[frame2Start + 2].Should().Be(0x00);
        written[frame2Start + 3].Should().Be(0x03);
    }

    [Fact]
    public async Task WriteFrameAsync_ConcurrentWrites_AreThreadSafe()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new SerialFrameWriter(stream);
        var tasks = new List<Task>();

        // Act - Write 10 frames concurrently
        for (int i = 0; i < 10; i++)
        {
            var payload = new byte[] { (byte)i };
            tasks.Add(writer.WriteFrameAsync(payload));
        }

        await Task.WhenAll(tasks);

        // Assert - All frames should be written without corruption
        var written = stream.ToArray();
        written.Length.Should().Be(10 * (FrameConstants.HeaderSize + 1));

        // Verify each frame header
        for (int i = 0; i < 10; i++)
        {
            var offset = i * (FrameConstants.HeaderSize + 1);
            written[offset].Should().Be(FrameConstants.Magic1);
            written[offset + 1].Should().Be(FrameConstants.Magic2);
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStream_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SerialFrameWriter(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
