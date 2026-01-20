using System.Collections.Concurrent;
using System.IO.Ports;
using Familiar.Meshtastic.Protocol;
using Meshtastic.Protobufs;
using Microsoft.Extensions.Logging;

namespace Familiar.Meshtastic.Connection;

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState)
    {
        OldState = oldState;
        NewState = newState;
    }

    public ConnectionState OldState { get; }
    public ConnectionState NewState { get; }
}

/// <summary>
/// Manages the connection to a Meshtastic device over serial.
/// Handles connection lifecycle, configuration sequence, and message routing.
/// </summary>
public class MeshtasticConnection : IDisposable
{
    private readonly ILogger<MeshtasticConnection> _logger;
    private readonly MeshtasticOptions _options;
    private readonly MessageSerializer _serializer;
    private readonly FromRadioDispatcher _dispatcher;

    private SerialPort? _serialPort;
    private SerialFrameReader? _frameReader;
    private SerialFrameWriter? _frameWriter;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;

    private ConnectionState _state = ConnectionState.Disconnected;
    private readonly object _stateLock = new();
    private uint _configRequestId;
    private TaskCompletionSource<bool>? _configCompleteTcs;

    private MyNodeInfo? _myNodeInfo;
    private readonly ConcurrentDictionary<uint, NodeInfo> _nodeDatabase = new();
    private readonly List<Channel> _channels = new();
    private DeviceMetadata? _deviceMetadata;

    private bool _disposed;

    public MeshtasticConnection(
        MeshtasticOptions options,
        ILogger<MeshtasticConnection> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serializer = new MessageSerializer(logger);
        _dispatcher = new FromRadioDispatcher(logger);

        // Wire up dispatcher events
        _dispatcher.MyInfoReceived += OnMyInfoReceived;
        _dispatcher.NodeInfoReceived += OnNodeInfoReceived;
        _dispatcher.ChannelReceived += OnChannelReceived;
        _dispatcher.ConfigComplete += OnConfigComplete;
        _dispatcher.MetadataReceived += OnMetadataReceived;
        _dispatcher.DeviceRebooted += OnDeviceRebooted;
        _dispatcher.PacketReceived += OnPacketReceived;
    }

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState State
    {
        get { lock (_stateLock) return _state; }
    }

    /// <summary>
    /// Gets whether the connection is fully established.
    /// </summary>
    public bool IsConnected => State == ConnectionState.Connected;

    /// <summary>
    /// Gets information about this node.
    /// </summary>
    public MyNodeInfo? MyNodeInfo => _myNodeInfo;

    /// <summary>
    /// Gets the node database.
    /// </summary>
    public IReadOnlyDictionary<uint, NodeInfo> NodeDatabase => _nodeDatabase;

    /// <summary>
    /// Gets the configured channels.
    /// </summary>
    public IReadOnlyList<Channel> Channels => _channels.AsReadOnly();

    /// <summary>
    /// Gets device metadata.
    /// </summary>
    public DeviceMetadata? DeviceMetadata => _deviceMetadata;

    /// <summary>
    /// Gets the FromRadio dispatcher for subscribing to events.
    /// </summary>
    public FromRadioDispatcher Dispatcher => _dispatcher;

    /// <summary>
    /// Raised when connection state changes.
    /// </summary>
    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Connects to the Meshtastic device.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if connected successfully.</returns>
    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (State != ConnectionState.Disconnected)
        {
            _logger.LogWarning("Cannot connect: already in state {State}", State);
            return false;
        }

        SetState(ConnectionState.Connecting);

        try
        {
            // Open serial port
            _serialPort = new SerialPort(_options.Port, _options.BaudRate)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _serialPort.Open();
            _logger.LogInformation("Opened serial port {Port} at {BaudRate} baud",
                _options.Port, _options.BaudRate);

            // Create frame handlers
            _frameReader = new SerialFrameReader(_serialPort.BaseStream, _logger);
            _frameWriter = new SerialFrameWriter(_serialPort.BaseStream, _logger);

            // Start read loop
            _readLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _readLoopTask = Task.Run(() => ReadLoopAsync(_readLoopCts.Token), _readLoopCts.Token);

            // Request configuration
            SetState(ConnectionState.Configuring);
            return await RequestConfigurationAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Meshtastic device");
            SetState(ConnectionState.Failed);
            await DisconnectAsync();
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the Meshtastic device.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (State == ConnectionState.Disconnected)
        {
            return;
        }

        SetState(ConnectionState.Disconnecting);

        try
        {
            // Stop read loop
            _readLoopCts?.Cancel();
            if (_readLoopTask != null)
            {
                try
                {
                    await _readLoopTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Read loop did not stop in time");
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            // Close serial port
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }

            _serialPort?.Dispose();
            _serialPort = null;
            _frameReader = null;
            _frameWriter = null;
            _readLoopCts?.Dispose();
            _readLoopCts = null;

            _logger.LogInformation("Disconnected from Meshtastic device");
        }
        finally
        {
            SetState(ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// Sends a ToRadio message to the device.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SendAsync(ToRadio message, CancellationToken ct = default)
    {
        if (_frameWriter == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        var data = _serializer.SerializeToRadio(message);
        await _frameWriter.WriteFrameAsync(data, ct);
    }

    /// <summary>
    /// Sends a MeshPacket to the device.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SendPacketAsync(MeshPacket packet, CancellationToken ct = default)
    {
        var message = MessageSerializer.CreatePacketMessage(packet);
        await SendAsync(message, ct);
    }

    /// <summary>
    /// Sends a text message over the mesh.
    /// </summary>
    /// <param name="text">The text to send.</param>
    /// <param name="destination">Destination node (0xFFFFFFFF for broadcast).</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="wantAck">Whether to request acknowledgment.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SendTextMessageAsync(
        string text,
        uint destination = FrameConstants.BroadcastAddress,
        uint channel = 0,
        bool wantAck = false,
        CancellationToken ct = default)
    {
        var packet = MessageSerializer.CreateTextMessagePacket(text, destination, channel, wantAck);
        await SendPacketAsync(packet, ct);
    }

    private async Task<bool> RequestConfigurationAsync(CancellationToken ct)
    {
        _configRequestId = (uint)Random.Shared.Next(1, int.MaxValue);
        _configCompleteTcs = new TaskCompletionSource<bool>();

        var request = MessageSerializer.CreateWantConfigRequest(_configRequestId);
        await SendAsync(request, ct);

        _logger.LogDebug("Sent config request with id {Id}", _configRequestId);

        // Wait for config complete with timeout
        var timeout = _options.ConfigTimeoutMs > 0 ? _options.ConfigTimeoutMs : 10000;
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            linkedCts.Token.Register(() => _configCompleteTcs.TrySetCanceled());
            await _configCompleteTcs.Task;

            SetState(ConnectionState.Connected);
            _logger.LogInformation("Meshtastic device connected and configured");
            return true;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("Configuration timeout after {Timeout}ms", timeout);
            SetState(ConnectionState.Failed);
            return false;
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        _logger.LogDebug("Read loop started");

        try
        {
            while (!ct.IsCancellationRequested && _frameReader != null)
            {
                var frame = await _frameReader.ReadFrameAsync(ct);
                if (frame == null)
                {
                    continue;
                }

                var message = _serializer.DeserializeFromRadio(frame);
                if (message == null)
                {
                    continue;
                }

                _dispatcher.Dispatch(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in read loop");
            SetState(ConnectionState.Failed);
        }

        _logger.LogDebug("Read loop stopped");
    }

    private void SetState(ConnectionState newState)
    {
        ConnectionState oldState;
        lock (_stateLock)
        {
            if (_state == newState) return;
            oldState = _state;
            _state = newState;
        }

        _logger.LogDebug("Connection state: {OldState} -> {NewState}", oldState, newState);
        StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState));
    }

    private void OnMyInfoReceived(object? sender, MyNodeInfoReceivedEventArgs e)
    {
        _myNodeInfo = e.MyInfo;
    }

    private void OnNodeInfoReceived(object? sender, NodeInfoReceivedEventArgs e)
    {
        _nodeDatabase[e.NodeInfo.Num] = e.NodeInfo;
    }

    private void OnChannelReceived(object? sender, ChannelReceivedEventArgs e)
    {
        // Ensure list has enough capacity
        while (_channels.Count <= e.Channel.Index)
        {
            _channels.Add(new Channel());
        }
        _channels[e.Channel.Index] = e.Channel;
    }

    private void OnConfigComplete(object? sender, ConfigCompleteEventArgs e)
    {
        if (e.ConfigId == _configRequestId)
        {
            _configCompleteTcs?.TrySetResult(true);
        }
    }

    private void OnMetadataReceived(object? sender, DeviceMetadataReceivedEventArgs e)
    {
        _deviceMetadata = e.Metadata;
    }

    private void OnDeviceRebooted(object? sender, EventArgs e)
    {
        // Device rebooted, need to reconfigure
        if (State == ConnectionState.Connected)
        {
            _logger.LogInformation("Device rebooted, requesting new configuration");
            SetState(ConnectionState.Configuring);
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // Give device time to initialize
                await RequestConfigurationAsync(CancellationToken.None);
            });
        }
    }

    private void OnPacketReceived(object? sender, MeshPacketReceivedEventArgs e)
    {
        // Update node database with last heard info
        if (e.Packet.From != 0 && _nodeDatabase.TryGetValue(e.Packet.From, out var existingNode))
        {
            // Update last heard time
            var updatedNode = new NodeInfo
            {
                Num = existingNode.Num,
                User = existingNode.User,
                Position = existingNode.Position,
                Snr = e.Packet.RxSnr,
                LastHeard = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DeviceMetrics = existingNode.DeviceMetrics,
                Channel = existingNode.Channel,
                ViaMqtt = existingNode.ViaMqtt,
                HopsAway = existingNode.HopsAway,
                IsFavorite = existingNode.IsFavorite
            };
            _nodeDatabase[e.Packet.From] = updatedNode;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dispatcher.MyInfoReceived -= OnMyInfoReceived;
        _dispatcher.NodeInfoReceived -= OnNodeInfoReceived;
        _dispatcher.ChannelReceived -= OnChannelReceived;
        _dispatcher.ConfigComplete -= OnConfigComplete;
        _dispatcher.MetadataReceived -= OnMetadataReceived;
        _dispatcher.DeviceRebooted -= OnDeviceRebooted;
        _dispatcher.PacketReceived -= OnPacketReceived;

        DisconnectAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
