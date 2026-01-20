namespace Familiar.Meshtastic.Connection;

/// <summary>
/// Represents the connection state of a Meshtastic device.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Not connected to any device.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connecting to the device (opening serial port).
    /// </summary>
    Connecting,

    /// <summary>
    /// Serial port is open, waiting for device configuration.
    /// </summary>
    Configuring,

    /// <summary>
    /// Fully connected and configured.
    /// </summary>
    Connected,

    /// <summary>
    /// Disconnecting from the device.
    /// </summary>
    Disconnecting,

    /// <summary>
    /// Connection failed.
    /// </summary>
    Failed
}
