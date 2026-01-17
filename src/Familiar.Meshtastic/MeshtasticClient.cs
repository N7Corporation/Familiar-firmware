using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Meshtastic;

/// <summary>
/// Client for communicating with a Meshtastic device over serial port.
/// </summary>
/// <remarks>
/// This is a simplified implementation that uses text-mode serial communication.
/// For full protobuf support, consider using the official Meshtastic libraries.
/// </remarks>
public class MeshtasticClient : IMeshtasticClient
{
    private readonly MeshtasticOptions _options;
    private readonly ILogger<MeshtasticClient> _logger;
    private SerialPort? _serialPort;
    private readonly List<MeshtasticNode> _knownNodes = new();
    private readonly StringBuilder _receiveBuffer = new();
    private bool _disposed;

    public MeshtasticClient(
        IOptions<MeshtasticOptions> options,
        ILogger<MeshtasticClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _serialPort?.IsOpen == true;

    public IReadOnlyList<MeshtasticNode> KnownNodes => _knownNodes.AsReadOnly();

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected)
        {
            return;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation("Meshtastic integration is disabled");
            return;
        }

        try
        {
            _serialPort = new SerialPort(_options.Port, _options.BaudRate)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                Encoding = Encoding.UTF8
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.ErrorReceived += OnErrorReceived;
            _serialPort.Open();

            _logger.LogInformation("Connected to Meshtastic device on {Port}", _options.Port);
            ConnectionStateChanged?.Invoke(this, true);

            // Give the device time to initialize
            await Task.Delay(100, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Meshtastic device on {Port}", _options.Port);
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        }
    }

    public Task DisconnectAsync()
    {
        if (_serialPort != null)
        {
            _serialPort.DataReceived -= OnDataReceived;
            _serialPort.ErrorReceived -= OnErrorReceived;

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.Dispose();
            _serialPort = null;

            _logger.LogInformation("Disconnected from Meshtastic device");
            ConnectionStateChanged?.Invoke(this, false);
        }

        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(string text, string? destinationNode = null, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to Meshtastic device");
        }

        // This is a simplified implementation
        // Full implementation would use protobuf encoding
        var message = destinationNode != null
            ? $"!sendto {destinationNode} {text}\n"
            : $"!send {text}\n";

        var data = Encoding.UTF8.GetBytes(message);
        await _serialPort!.BaseStream.WriteAsync(data, ct);
        await _serialPort.BaseStream.FlushAsync(ct);

        _logger.LogDebug("Sent message: {Message}", text);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            return;
        }

        try
        {
            var data = _serialPort.ReadExisting();
            _receiveBuffer.Append(data);

            // Process complete lines
            var content = _receiveBuffer.ToString();
            var lines = content.Split('\n');

            // Keep incomplete line in buffer
            _receiveBuffer.Clear();
            if (!content.EndsWith('\n') && lines.Length > 0)
            {
                _receiveBuffer.Append(lines[^1]);
                lines = lines[..^1];
            }

            foreach (var line in lines)
            {
                ProcessLine(line.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from serial port");
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        _logger.LogError("Serial port error: {Error}", e.EventType);
    }

    private void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _logger.LogTrace("Received line: {Line}", line);

        // Parse received messages
        // Format varies by firmware, this is a simplified example
        if (line.StartsWith("MSG:") || line.Contains("received:"))
        {
            try
            {
                var message = ParseMessage(line);
                if (message != null && ShouldProcessMessage(message))
                {
                    _logger.LogInformation("Message from {Node}: {Text}",
                        message.FromNode, message.Text);

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse message: {Line}", line);
            }
        }
    }

    private MeshtasticMessage? ParseMessage(string line)
    {
        // This is a simplified parser
        // Real implementation would use protobuf deserialization

        // Example format: "MSG: !abc123 -> !def456: Hello world"
        var parts = line.Split(new[] { "->", ":" }, StringSplitOptions.TrimEntries);

        if (parts.Length >= 3)
        {
            return new MeshtasticMessage
            {
                FromNode = ExtractNodeId(parts[0]),
                ToNode = ExtractNodeId(parts[1]),
                Text = string.Join(":", parts.Skip(2)).Trim(),
                Channel = _options.Channel,
                ReceivedAt = DateTime.UtcNow
            };
        }

        return null;
    }

    private static string ExtractNodeId(string text)
    {
        // Extract node ID (e.g., "!abc123" from "MSG: !abc123")
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

    private bool ShouldProcessMessage(MeshtasticMessage message)
    {
        // Check if message is from an allowed node
        if (_options.AllowedNodes.Count > 0 &&
            !_options.AllowedNodes.Contains(message.FromNode))
        {
            _logger.LogDebug("Ignoring message from non-allowed node: {Node}", message.FromNode);
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DisconnectAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
