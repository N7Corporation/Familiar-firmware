# Screen Display Feature

## Overview

The screen display feature adds a small screen to the cosplayer's device for silent visual communication. This allows handlers to send text messages, status updates, and alerts that the cosplayer can read without audio.

## Use Cases

1. **Silent Communication** - Send messages when audio would break character or isn't appropriate
2. **Status Display** - Show battery level, connection status, time
3. **Cue Cards** - Display lines or reminders for performances
4. **Photo Countdown** - Visual countdown before photo is taken
5. **Navigation** - Show directions or booth numbers at conventions

## Hardware Options

### Recommended Displays

| Display | Size | Resolution | Interface | Notes |
|---------|------|------------|-----------|-------|
| SSD1306 OLED | 0.96" - 1.3" | 128x64 | I2C/SPI | Low power, high contrast, great for text |
| SH1106 OLED | 1.3" | 128x64 | I2C/SPI | Similar to SSD1306, slightly larger |
| ST7789 LCD | 1.3" - 2.0" | 240x240 | SPI | Color display, good for images |
| ILI9341 LCD | 2.4" - 3.2" | 320x240 | SPI | Larger color display |
| E-ink | 1.5" - 2.9" | Various | SPI | No power when static, outdoor readable |

### Recommended for Familiar

**Primary: SSD1306 1.3" OLED (I2C)**
- Low power consumption
- High contrast (readable in dark venues)
- Simple wiring (4 wires: VCC, GND, SDA, SCL)
- Inexpensive (~$5-10)
- Small enough to hide in costume

**Alternative: E-ink for outdoor events**
- Readable in direct sunlight
- Zero power when displaying static content
- Slower refresh rate (not for animations)

## Wiring

### SSD1306 OLED (I2C)

```
OLED Pin    Pi Pin
--------    ------
VCC    -->  3.3V (Pin 1)
GND    -->  GND (Pin 6)
SDA    -->  GPIO 2 / SDA1 (Pin 3)
SCL    -->  GPIO 3 / SCL1 (Pin 5)
```

### Enable I2C on Raspberry Pi

```bash
sudo raspi-config
# Interface Options -> I2C -> Enable
sudo reboot
```

Verify device is detected:
```bash
sudo i2cdetect -y 1
# Should show device at 0x3C or 0x3D
```

## Software Architecture

### Configuration

```json
{
  "Familiar": {
    "Screen": {
      "Enabled": true,
      "Type": "SSD1306",
      "Width": 128,
      "Height": 64,
      "I2CAddress": "0x3C",
      "Rotation": 0,
      "Brightness": 255,
      "IdleTimeout": 30,
      "DefaultView": "status"
    }
  }
}
```

### Display Views

1. **Status View** (default)
   ```
   ┌────────────────────┐
   │ Familiar    12:34  │
   │ ─────────────────  │
   │ WiFi: Connected    │
   │ Mesh: 3 nodes      │
   │ Batt: 85% ████▒    │
   └────────────────────┘
   ```

2. **Message View**
   ```
   ┌────────────────────┐
   │ From: Handler      │
   │ ─────────────────  │
   │ Photo time!        │
   │ Hold your pose     │
   │                    │
   └────────────────────┘
   ```

3. **Alert View**
   ```
   ┌────────────────────┐
   │    ⚠ ALERT ⚠      │
   │                    │
   │  Someone behind    │
   │      you!          │
   │                    │
   └────────────────────┘
   ```

4. **Countdown View**
   ```
   ┌────────────────────┐
   │                    │
   │        3          │
   │                    │
   │   Photo in...      │
   │                    │
   └────────────────────┘
   ```

### API Endpoints

```
GET  /api/screen/status
POST /api/screen/message
POST /api/screen/alert
POST /api/screen/countdown
POST /api/screen/clear
POST /api/screen/brightness
GET  /api/screen/views
POST /api/screen/view/{viewName}
```

### Message API

```http
POST /api/screen/message
Content-Type: application/json

{
  "text": "Photo time! Hold your pose",
  "duration": 10,
  "priority": "normal",
  "sound": false
}
```

### Alert API

```http
POST /api/screen/alert
Content-Type: application/json

{
  "text": "Someone behind you!",
  "level": "warning",
  "vibrate": true,
  "duration": 5
}
```

Alert levels: `info`, `warning`, `urgent`

### Countdown API

```http
POST /api/screen/countdown
Content-Type: application/json

{
  "seconds": 3,
  "message": "Photo in...",
  "endMessage": "SMILE!",
  "vibrate": true
}
```

## Implementation

### Interface

```csharp
public interface IScreenService
{
    bool IsAvailable { get; }
    int Width { get; }
    int Height { get; }

    Task InitializeAsync(CancellationToken ct = default);
    Task ShowMessageAsync(string text, int durationSeconds = 5, CancellationToken ct = default);
    Task ShowAlertAsync(string text, AlertLevel level, CancellationToken ct = default);
    Task ShowCountdownAsync(int seconds, string? message = null, CancellationToken ct = default);
    Task ShowStatusAsync(CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    Task SetBrightnessAsync(byte brightness, CancellationToken ct = default);
}
```

### Dependencies

```xml
<PackageReference Include="Iot.Device.Bindings" Version="3.*" />
```

The `Iot.Device.Bindings` package includes drivers for SSD1306, SH1106, and other common displays.

## Web UI Integration

Add screen controls to the handler interface:

```html
<!-- Message input -->
<div class="screen-controls">
  <input type="text" id="screen-message" placeholder="Message for cosplayer...">
  <button onclick="sendScreenMessage()">Send to Screen</button>
</div>

<!-- Quick alerts -->
<div class="screen-alerts">
  <button onclick="sendAlert('behind')">Behind you!</button>
  <button onclick="sendAlert('left')">Left side!</button>
  <button onclick="sendAlert('right')">Right side!</button>
  <button onclick="sendCountdown(3)">Photo 3s</button>
</div>
```

## Power Considerations

| Display | Active | Idle | Sleep |
|---------|--------|------|-------|
| SSD1306 OLED | ~20mA | ~20mA | <1mA |
| ST7789 LCD | ~40mA | ~40mA | ~5mA |
| E-ink | ~25mA | 0mA | 0mA |

OLED power depends on how many pixels are lit. White text on black background uses less power than inverted.

## Future Enhancements

- [ ] Custom fonts and sizes
- [ ] Image/icon display
- [ ] Animations for alerts
- [ ] Multiple display support
- [ ] Touch screen input
- [ ] QR code display for WiFi sharing
