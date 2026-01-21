# Meshtastic UART/I2C Communication

## Overview

This document describes implementing direct UART or I2C communication with the Meshtastic radio module instead of USB serial. Direct hardware communication provides several advantages for embedded integration.

## Why UART/I2C Instead of USB?

| Aspect | USB Serial | UART/I2C |
|--------|-----------|----------|
| Connection reliability | USB enumeration can fail | Direct hardware, always available |
| Power consumption | USB hub power overhead | Lower power draw |
| Boot dependency | Waits for USB enumeration | Available immediately |
| Cable requirements | USB cable + connector | PCB traces or short wires |
| Hot-plug complexity | Handled by OS | N/A (hardwired) |
| Integration | External module | On-board integration possible |

## Recommended Approach: UART

UART is the preferred interface because:
- Meshtastic firmware already supports serial protobuf over UART
- No firmware modifications required
- Simple two-wire connection (TX/RX)
- Well-supported on Raspberry Pi

### Hardware Connection

```
Raspberry Pi GPIO          Meshtastic Module (ESP32/nRF52)
┌─────────────────┐       ┌─────────────────────────────────┐
│                 │       │                                 │
│  GPIO 14 (TXD) ─┼───────┼─► RX                            │
│  GPIO 15 (RXD) ◄┼───────┼── TX                            │
│  GND ───────────┼───────┼── GND                           │
│  3.3V ──────────┼───────┼── 3.3V (if module supports)     │
│                 │       │                                 │
└─────────────────┘       └─────────────────────────────────┘
```

### Raspberry Pi UART Configuration

1. **Enable UART in `/boot/config.txt`**:
```ini
# Disable Bluetooth to free up PL011 UART (more reliable than mini-UART)
dtoverlay=disable-bt

# Enable UART
enable_uart=1
```

2. **Disable serial console in `/boot/cmdline.txt`**:
Remove `console=serial0,115200` from the kernel command line.

3. **Disable serial-getty service**:
```bash
sudo systemctl disable serial-getty@ttyAMA0.service
```

4. **Reboot** to apply changes.

The UART will be available at `/dev/ttyAMA0`.

### Meshtastic Module Configuration

Most Meshtastic modules expose UART pins on their GPIO headers:

| Module | TX Pin | RX Pin | Notes |
|--------|--------|--------|-------|
| Heltec V3 | GPIO 43 | GPIO 44 | Secondary UART |
| T-Beam | GPIO 34 | GPIO 35 | Exposed on header |
| RAK4631 | P0.06 | P0.08 | UART1 pins |
| LilyGo T-Echo | GPIO 0.24 | GPIO 0.25 | Accessible |

Configure the module to use Serial over the appropriate UART port via Meshtastic CLI or Python API:
```bash
meshtastic --set serial.enabled true
meshtastic --set serial.rxd <pin>
meshtastic --set serial.txd <pin>
meshtastic --set serial.baud 115200
meshtastic --set serial.mode PROTO
```

## Alternative: I2C (Requires Firmware Modification)

I2C communication is not natively supported in Meshtastic firmware. Implementation would require:

### Custom Firmware Approach

1. **Add I2C Slave Mode to Meshtastic**:
   - Implement I2C slave driver for ESP32/nRF52
   - Create protobuf message framing over I2C
   - Handle I2C clock stretching for slow operations

2. **I2C Protocol Design**:
```
┌─────────────────────────────────────────────────────────┐
│ I2C Frame Format                                         │
├──────────┬──────────┬─────────────────┬─────────────────┤
│ Length   │ Type     │ Protobuf Data   │ CRC16           │
│ (2 bytes)│ (1 byte) │ (variable)      │ (2 bytes)       │
└──────────┴──────────┴─────────────────┴─────────────────┘

Type values:
  0x01 = ToRadio message
  0x02 = FromRadio message
  0x03 = Config request
  0x04 = Config response
```

3. **Register Map** (for I2C communication):

| Register | Address | Size | Description |
|----------|---------|------|-------------|
| STATUS | 0x00 | 1 | Flags: RX_READY, TX_BUSY, ERROR |
| RX_LEN | 0x01 | 2 | Length of pending RX message |
| RX_DATA | 0x03 | var | Read RX message data |
| TX_LEN | 0x80 | 2 | Set TX message length |
| TX_DATA | 0x82 | var | Write TX message data |
| COMMAND | 0xF0 | 1 | Send command (0x01=SEND, 0x02=CLEAR) |

**Recommendation**: Unless there's a specific need for I2C (e.g., limited GPIO), UART is strongly preferred as it requires no firmware changes.

## Software Implementation

### Update MeshtasticOptions

```csharp
public class MeshtasticOptions
{
    // Existing options...

    /// <summary>
    /// Communication interface type
    /// </summary>
    public MeshtasticInterface Interface { get; set; } = MeshtasticInterface.Serial;

    /// <summary>
    /// Serial port path (e.g., /dev/ttyAMA0 for UART, /dev/ttyUSB0 for USB)
    /// </summary>
    public string Port { get; set; } = "/dev/ttyAMA0";
}

public enum MeshtasticInterface
{
    Serial,  // USB or UART serial
    I2C      // Future: I2C interface
}
```

### Configuration

Update `appsettings.json` for UART:
```json
{
  "Familiar": {
    "Meshtastic": {
      "Enabled": true,
      "Interface": "Serial",
      "Port": "/dev/ttyAMA0",
      "BaudRate": 115200
    }
  }
}
```

### MeshtasticService Changes

The existing `MeshtasticService` already uses serial communication. Changes needed:

1. **Auto-detect Interface Type**:
```csharp
private string DetectInterface()
{
    // Check for hardware UART first (preferred for reliability)
    if (File.Exists("/dev/ttyAMA0"))
    {
        _logger.LogInformation("Using hardware UART: /dev/ttyAMA0");
        return "/dev/ttyAMA0";
    }

    // Fall back to USB serial
    var usbPorts = Directory.GetFiles("/dev", "ttyUSB*")
        .Concat(Directory.GetFiles("/dev", "ttyACM*"))
        .OrderBy(p => p)
        .ToList();

    if (usbPorts.Count > 0)
    {
        _logger.LogInformation("Using USB serial: {Port}", usbPorts[0]);
        return usbPorts[0];
    }

    throw new InvalidOperationException("No Meshtastic device found");
}
```

2. **Handle UART-specific initialization**:
```csharp
private void ConfigureSerialPort(string port)
{
    // UART on Pi doesn't need special configuration
    // USB serial may need DTR/RTS handling

    _serialPort = new SerialPort(port, _options.BaudRate)
    {
        ReadTimeout = 1000,
        WriteTimeout = 1000
    };

    // For USB devices, toggle DTR to reset the module
    if (port.Contains("USB") || port.Contains("ACM"))
    {
        _serialPort.DtrEnable = true;
    }

    _serialPort.Open();
}
```

## PCB Integration

For a custom mainboard, integrate the Meshtastic module directly:

### Option 1: Castellated Module

Use a castellated Meshtastic module (like RAK4631) that can be soldered directly to the mainboard:

```
┌─────────────────────────────────────────────────────────────┐
│                      FAMILIAR MAINBOARD                      │
│                                                             │
│   ┌─────────────────┐        ┌────────────────────────┐     │
│   │                 │        │                        │     │
│   │  Raspberry Pi   │        │   RAK4631 Module       │     │
│   │  CM4 / Pi 5     │        │   (castellated edges)  │     │
│   │                 │        │                        │     │
│   │      UART ──────┼────────┼──► RX/TX               │     │
│   │      3V3 ───────┼────────┼──► VCC                 │     │
│   │      GND ───────┼────────┼──► GND                 │     │
│   │                 │        │                        │     │
│   └─────────────────┘        │   ◄── SMA Antenna      │     │
│                              └────────────────────────┘     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Option 2: Module Socket

Use a socket/header for swappable Meshtastic modules:

- 2.54mm pin headers
- Allows module replacement/upgrade
- Useful for development and testing

### Required Connections

| Signal | Pi GPIO | Module Pin | Notes |
|--------|---------|------------|-------|
| TX | GPIO 14 | RX | 3.3V logic |
| RX | GPIO 15 | TX | 3.3V logic |
| GND | GND | GND | Common ground |
| VCC | 3.3V | VCC | Check module voltage! |
| RESET | GPIO 17 | RESET | Optional, for hard reset |
| BOOT | GPIO 27 | BOOT0 | Optional, for firmware update |

### Level Shifting

Both Raspberry Pi and most Meshtastic modules (ESP32, nRF52) use 3.3V logic, so no level shifting is required. Verify your specific module's voltage requirements.

## Testing

### Verify UART Communication

```bash
# Check UART is available
ls -la /dev/ttyAMA0

# Test loopback (connect TX to RX temporarily)
echo "test" > /dev/ttyAMA0
cat /dev/ttyAMA0

# Monitor Meshtastic traffic
stty -F /dev/ttyAMA0 115200 raw -echo
cat /dev/ttyAMA0 | xxd
```

### Verify with Meshtastic CLI

```bash
# Install Meshtastic CLI
pip install meshtastic

# Test connection via UART
meshtastic --port /dev/ttyAMA0 --info
```

### Integration Test

```bash
# Run Familiar firmware
dotnet run --project src/Familiar.Host

# Check logs for Meshtastic connection
# Should see: "Connected to Meshtastic device on /dev/ttyAMA0"
```

## Troubleshooting

### No Communication

1. Check UART is enabled: `dmesg | grep ttyAMA`
2. Verify permissions: `sudo usermod -a -G dialout $USER`
3. Check wiring: TX→RX, RX→TX (crossover)
4. Verify baud rate matches (115200)

### Garbled Data

1. Ensure serial console is disabled
2. Check for baud rate mismatch
3. Verify 3.3V logic levels

### Module Not Responding

1. Check module power supply (3.3V stable)
2. Try reset pin toggle
3. Verify module firmware has serial enabled

## Future Considerations

- **SPI Interface**: Some nRF52-based modules support SPI for higher throughput
- **Direct BLE**: Connect to Meshtastic via BLE from Pi (requires BlueZ configuration)
- **Custom Protocol**: For maximum efficiency, implement a minimal binary protocol instead of full protobuf

## References

- [Meshtastic Serial Interface Documentation](https://meshtastic.org/docs/configuration/module/serial)
- [Raspberry Pi UART Configuration](https://www.raspberrypi.com/documentation/computers/configuration.html#configuring-uarts)
- [Meshtastic Protobuf Definitions](https://github.com/meshtastic/protobufs)
