using Microsoft.Extensions.Logging;

namespace Familiar.Meshtastic.Protocol;

/// <summary>
/// Writes framed protobuf messages to a serial stream.
/// Frame format: 0x94 0xC3 MSB LSB [payload...]
/// </summary>
public class SerialFrameWriter
{
    private readonly Stream _stream;
    private readonly ILogger? _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SerialFrameWriter(Stream stream, ILogger? logger = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _logger = logger;
    }

    /// <summary>
    /// Writes a framed payload to the stream.
    /// </summary>
    /// <param name="payload">The payload bytes to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">If payload exceeds maximum size.</exception>
    public async Task WriteFrameAsync(byte[] payload, CancellationToken ct = default)
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (payload.Length > FrameConstants.MaxPayloadSize)
        {
            throw new ArgumentException(
                $"Payload size {payload.Length} exceeds maximum {FrameConstants.MaxPayloadSize}",
                nameof(payload));
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            // Build frame
            var frame = new byte[FrameConstants.HeaderSize + payload.Length];
            frame[0] = FrameConstants.Magic1;
            frame[1] = FrameConstants.Magic2;
            frame[2] = (byte)(payload.Length >> 8);   // MSB
            frame[3] = (byte)(payload.Length & 0xFF); // LSB
            Array.Copy(payload, 0, frame, FrameConstants.HeaderSize, payload.Length);

            // Write frame
            await _stream.WriteAsync(frame, ct);
            await _stream.FlushAsync(ct);

            _logger?.LogTrace("Wrote frame with {Length} bytes", payload.Length);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Writes a framed payload from a ReadOnlyMemory.
    /// </summary>
    /// <param name="payload">The payload to write.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task WriteFrameAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (payload.Length > FrameConstants.MaxPayloadSize)
        {
            throw new ArgumentException(
                $"Payload size {payload.Length} exceeds maximum {FrameConstants.MaxPayloadSize}",
                nameof(payload));
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            // Write header
            var header = new byte[FrameConstants.HeaderSize];
            header[0] = FrameConstants.Magic1;
            header[1] = FrameConstants.Magic2;
            header[2] = (byte)(payload.Length >> 8);   // MSB
            header[3] = (byte)(payload.Length & 0xFF); // LSB

            await _stream.WriteAsync(header, ct);
            await _stream.WriteAsync(payload, ct);
            await _stream.FlushAsync(ct);

            _logger?.LogTrace("Wrote frame with {Length} bytes", payload.Length);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
