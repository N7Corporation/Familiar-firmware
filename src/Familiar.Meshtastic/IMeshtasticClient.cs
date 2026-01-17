namespace Familiar.Meshtastic;

/// <summary>
/// Interface for Meshtastic device communication.
/// </summary>
public interface IMeshtasticClient : IDisposable
{
    /// <summary>
    /// Gets whether the client is connected to the Meshtastic device.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the list of known nodes on the mesh network.
    /// </summary>
    IReadOnlyList<MeshtasticNode> KnownNodes { get; }

    /// <summary>
    /// Event raised when a text message is received.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Connects to the Meshtastic device.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the Meshtastic device.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a text message to the mesh network.
    /// </summary>
    /// <param name="text">Message text to send.</param>
    /// <param name="destinationNode">Optional destination node ID. Null = broadcast.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendMessageAsync(string text, string? destinationNode = null, CancellationToken ct = default);
}
