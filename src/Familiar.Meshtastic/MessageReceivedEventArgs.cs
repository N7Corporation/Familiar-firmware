namespace Familiar.Meshtastic;

/// <summary>
/// Event arguments for when a message is received from the mesh network.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(MeshtasticMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// The received message.
    /// </summary>
    public MeshtasticMessage Message { get; }

    /// <summary>
    /// The node ID that sent the message.
    /// </summary>
    public string FromNode => Message.FromNode;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Text => Message.Text;
}
