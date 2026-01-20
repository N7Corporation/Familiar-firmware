using Microsoft.Extensions.Logging;

namespace Familiar.Meshtastic.Protocol;

/// <summary>
/// Reads framed protobuf messages from a serial stream.
/// Frame format: 0x94 0xC3 MSB LSB [payload...]
/// </summary>
public class SerialFrameReader
{
    private readonly Stream _stream;
    private readonly ILogger? _logger;
    private readonly byte[] _headerBuffer = new byte[FrameConstants.HeaderSize];
    private readonly byte[] _payloadBuffer = new byte[FrameConstants.MaxPayloadSize];

    public SerialFrameReader(Stream stream, ILogger? logger = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _logger = logger;
    }

    /// <summary>
    /// Reads a single frame from the stream.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The frame payload, or null if no valid frame found before timeout/cancellation.</returns>
    public async Task<byte[]?> ReadFrameAsync(CancellationToken ct = default)
    {
        try
        {
            // Sync to frame header
            if (!await SyncToHeaderAsync(ct))
            {
                return null;
            }

            // Read length (big-endian)
            int bytesRead = await ReadExactlyAsync(_headerBuffer, 2, 2, ct);
            if (bytesRead < 2)
            {
                _logger?.LogDebug("Failed to read frame length");
                return null;
            }

            int payloadLength = (_headerBuffer[2] << 8) | _headerBuffer[3];

            if (payloadLength <= 0 || payloadLength > FrameConstants.MaxPayloadSize)
            {
                _logger?.LogWarning("Invalid frame length: {Length}", payloadLength);
                return null;
            }

            // Read payload
            bytesRead = await ReadExactlyAsync(_payloadBuffer, 0, payloadLength, ct);
            if (bytesRead < payloadLength)
            {
                _logger?.LogDebug("Failed to read full payload: got {Got} of {Expected}", bytesRead, payloadLength);
                return null;
            }

            var payload = new byte[payloadLength];
            Array.Copy(_payloadBuffer, 0, payload, 0, payloadLength);

            _logger?.LogTrace("Read frame with {Length} bytes", payloadLength);
            return payload;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (IOException ex)
        {
            _logger?.LogDebug(ex, "IO error reading frame");
            return null;
        }
    }

    /// <summary>
    /// Synchronizes to the frame header magic bytes.
    /// </summary>
    private async Task<bool> SyncToHeaderAsync(CancellationToken ct)
    {
        int syncAttempts = 0;
        const int maxSyncAttempts = 1024; // Prevent infinite loop on garbage data

        while (!ct.IsCancellationRequested && syncAttempts < maxSyncAttempts)
        {
            // Read first magic byte
            int bytesRead = await ReadExactlyAsync(_headerBuffer, 0, 1, ct);
            if (bytesRead == 0)
            {
                return false;
            }

            if (_headerBuffer[0] != FrameConstants.Magic1)
            {
                syncAttempts++;
                continue;
            }

            // Read second magic byte
            bytesRead = await ReadExactlyAsync(_headerBuffer, 1, 1, ct);
            if (bytesRead == 0)
            {
                return false;
            }

            if (_headerBuffer[1] == FrameConstants.Magic2)
            {
                // Found header
                return true;
            }

            // Check if the byte we just read could be the start of a new header
            if (_headerBuffer[1] == FrameConstants.Magic1)
            {
                _headerBuffer[0] = _headerBuffer[1];
                syncAttempts++;
                continue;
            }

            syncAttempts++;
        }

        _logger?.LogWarning("Failed to sync to frame header after {Attempts} attempts", syncAttempts);
        return false;
    }

    /// <summary>
    /// Reads exactly the specified number of bytes from the stream.
    /// </summary>
    private async Task<int> ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;

        while (totalRead < count && !ct.IsCancellationRequested)
        {
            int bytesRead = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);

            if (bytesRead == 0)
            {
                // End of stream or timeout
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead;
    }
}
