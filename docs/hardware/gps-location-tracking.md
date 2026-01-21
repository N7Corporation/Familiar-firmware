# GPS Location Tracking Hardware

## Overview

This document describes the hardware implementation for location tracking using Meshtastic's built-in GPS capabilities. GPS enables handlers to locate cosplayers in crowded convention venues.

## GPS-Enabled Meshtastic Modules

Several Meshtastic-compatible modules include integrated GPS receivers:

| Module | GPS Chip | Sensitivity | Cold Start | Hot Start | Notes |
|--------|----------|-------------|------------|-----------|-------|
| LILYGO T-Beam v1.1+ | NEO-6M | -161 dBm | 27s | 1s | Most popular, good battery life |
| LILYGO T-Beam Supreme | NEO-M9N | -167 dBm | 24s | 1s | Multi-GNSS (GPS+GLONASS+Galileo) |
| RAK WisBlock + GPS | u-blox MAX-7Q | -161 dBm | 29s | 1s | Modular, stackable |
| Heltec V3 + GPS | External | Varies | Varies | Varies | Requires add-on GPS module |

### Recommended: LILYGO T-Beam Supreme

For the Familiar device, the T-Beam Supreme is recommended because:
- Multi-constellation GNSS (better indoor accuracy)
- Integrated 18650 battery holder
- Good power management
- Active community support
- Castellated edges available for PCB integration

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           FAMILIAR DEVICE                                │
│                                                                         │
│   ┌─────────────────┐         ┌─────────────────────────────────────┐  │
│   │                 │  UART   │         Meshtastic Module           │  │
│   │  Raspberry Pi   │◄───────►│  ┌─────────────┐  ┌──────────────┐  │  │
│   │                 │         │  │   LoRa      │  │    GPS       │  │  │
│   │  - Parse GPS    │         │  │   Radio     │  │   Receiver   │  │  │
│   │  - Store history│         │  │             │  │              │  │  │
│   │  - Serve API    │         │  └──────┬──────┘  └──────┬───────┘  │  │
│   │                 │         │         │                │          │  │
│   └─────────────────┘         │         ▼                ▼          │  │
│                               │    ┌─────────┐      ┌─────────┐     │  │
│                               │    │  LoRa   │      │  GPS    │     │  │
│                               │    │ Antenna │      │ Antenna │     │  │
│                               │    └─────────┘      └─────────┘     │  │
│                               └─────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Hardware Integration Options

### Option 1: Standalone Module (Recommended for DIY)

Use a complete T-Beam module connected via UART:

```
Raspberry Pi                    T-Beam Module
┌──────────────┐               ┌──────────────────────┐
│              │               │                      │
│  GPIO 14 ────┼───────────────┼──► RX                │
│  GPIO 15 ◄───┼───────────────┼─── TX                │
│  GND ────────┼───────────────┼─── GND               │
│              │               │                      │
│              │               │  [18650 Battery]     │
│              │               │  [GPS Antenna]       │
│              │               │  [LoRa Antenna]      │
└──────────────┘               └──────────────────────┘
```

**Pros**: Simple, self-contained, has own battery
**Cons**: Larger form factor, separate enclosure needed

### Option 2: Module-on-Board (Commercial Product)

Solder a castellated Meshtastic module directly to the mainboard:

```
┌─────────────────────────────────────────────────────────────────┐
│                      FAMILIAR MAINBOARD                          │
│                                                                 │
│  ┌─────────────┐    ┌─────────────────────────────────────────┐ │
│  │             │    │         GPS/LoRa Module                  │ │
│  │ Raspberry   │    │  ┌───────────┐   ┌───────────────────┐  │ │
│  │ Pi CM4      │    │  │ SX1262    │   │ NEO-M9N GPS       │  │ │
│  │             │    │  │ LoRa      │   │                   │  │ │
│  │ UART ───────┼────┼──┤           │   │                   │  │ │
│  │             │    │  └─────┬─────┘   └─────────┬─────────┘  │ │
│  └─────────────┘    │        │                   │            │ │
│                     └────────┼───────────────────┼────────────┘ │
│                              │                   │              │
│                        ┌─────┴─────┐       ┌─────┴─────┐        │
│                        │LoRa U.FL  │       │GPS U.FL   │        │
│                        └───────────┘       └───────────┘        │
│                                                                 │
│   To external antennas mounted on cosplay prop/costume          │
└─────────────────────────────────────────────────────────────────┘
```

### Option 3: Separate GPS Module

For Meshtastic modules without GPS (Heltec V3), add an external GPS:

| GPS Module | Interface | Size | Price | Notes |
|------------|-----------|------|-------|-------|
| BN-220 | UART | 22×20mm | ~$12 | Popular, good sensitivity |
| Beitian BN-880 | UART/I2C | 28×28mm | ~$15 | Includes compass |
| u-blox NEO-6M | UART | 25×25mm | ~$8 | Budget option |
| u-blox NEO-M9N | UART | 16×12mm | ~$25 | Best accuracy |

Connection to Meshtastic module:
```
GPS Module          Meshtastic (Heltec V3)
┌─────────────┐    ┌─────────────────────┐
│ TX ─────────┼────┼──► GPS_RX (GPIO 34) │
│ RX ─────────┼────┼─── GPS_TX (GPIO 12) │
│ VCC ────────┼────┼─── 3.3V             │
│ GND ────────┼────┼─── GND              │
└─────────────┘    └─────────────────────┘
```

## GPS Antenna Considerations

### Antenna Types

| Type | Gain | Size | Best For |
|------|------|------|----------|
| Chip antenna | 0-2 dBi | 5×5mm | Compact builds, good sky view |
| Patch antenna | 3-5 dBi | 25×25mm | Standard use, moderate sky view |
| Active antenna | 25-30 dB | 25×25mm+ | Indoor use, obstructed view |
| Helical antenna | 3 dBi | 10×30mm | Vertical mounting, 360° coverage |

### Placement Guidelines

```
GOOD: Clear sky view                BAD: Obstructed
┌─────────────────┐                ┌─────────────────┐
│   [GPS Antenna] │                │ ████████████████│
│        ↑        │                │   [GPS Antenna] │
│     Sky View    │                │        ↑        │
│                 │                │     Blocked     │
│   ┌─────────┐   │                │   ┌─────────┐   │
│   │ Device  │   │                │   │ Device  │   │
│   └─────────┘   │                │   └─────────┘   │
└─────────────────┘                └─────────────────┘
```

**Cosplay Integration Tips:**
- Mount GPS antenna on top of headpieces/helmets
- Use flexible antenna cables for costume movement
- Keep antenna away from metal and electronics
- Consider external patch antenna in a prop

### U.FL Connector

For PCB integration, use U.FL (IPEX) connectors:

| Parameter | Specification |
|-----------|---------------|
| Impedance | 50Ω |
| Frequency | DC - 6GHz |
| Mating cycles | 30 (use pigtail for frequent disconnects) |
| Cable | 1.13mm coax, keep under 15cm for GPS |

## Power Considerations

### GPS Module Power Consumption

| State | Current Draw | Notes |
|-------|--------------|-------|
| Cold start acquisition | 40-50 mA | First fix, can take 30+ seconds |
| Tracking | 25-35 mA | Normal operation |
| Power save mode | 10-15 mA | Periodic fixes |
| Backup mode | 10-20 µA | RTC only, fast warm start |

### Power Management Strategy

1. **Continuous tracking**: Best accuracy, ~35mA constant draw
2. **Periodic updates**: Fix every 30-60s, average ~15mA
3. **On-demand**: Only when requested, minimal power but slow first fix

Recommended configuration in Meshtastic:
```bash
# Set GPS update interval (seconds)
meshtastic --set position.gps_update_interval 30

# Enable power saving (fix then sleep)
meshtastic --set position.gps_mode ENABLED

# Set position broadcast interval
meshtastic --set position.position_broadcast_secs 60
```

## Indoor Performance

GPS performance degrades significantly indoors. Mitigation strategies:

### 1. Multi-Constellation GNSS

Use modules supporting multiple satellite systems:
- GPS (USA) - 31 satellites
- GLONASS (Russia) - 24 satellites
- Galileo (EU) - 30 satellites
- BeiDou (China) - 35 satellites

More satellites = better indoor penetration.

### 2. Dead Reckoning (Future Enhancement)

Add IMU for position estimation when GPS is unavailable:

```
┌─────────────────────────────────────────────┐
│              Enhanced Location              │
│                                             │
│   ┌─────────┐    ┌─────────┐    ┌────────┐  │
│   │  GPS    │    │   IMU   │    │ Fusion │  │
│   │ Module  │───►│ MPU6050 │───►│ Filter │──┼──► Position
│   └─────────┘    └─────────┘    └────────┘  │
│                                             │
│   GPS provides absolute position            │
│   IMU provides relative motion              │
│   Kalman filter combines both               │
└─────────────────────────────────────────────┘
```

### 3. WiFi/BLE Positioning (Future Enhancement)

Use convention venue WiFi for indoor positioning:
- Requires venue WiFi map
- 3-10 meter accuracy typical
- Falls back when GPS unavailable

## Bill of Materials

### Option 1: T-Beam Integration

| Qty | Component | Part Number | Price | Notes |
|-----|-----------|-------------|-------|-------|
| 1 | T-Beam Supreme | LILYGO T-Beam Supreme | ~$45 | With GPS |
| 1 | LoRa antenna | 868/915MHz SMA | ~$5 | Region dependent |
| 1 | GPS antenna | Active 28dB SMA | ~$8 | Optional upgrade |
| 1 | SMA pigtail | U.FL to SMA 15cm | ~$3 | If using external |

### Option 2: Separate GPS Module

| Qty | Component | Part Number | Price | Notes |
|-----|-----------|-------------|-------|-------|
| 1 | GPS Module | BN-220 or NEO-M9N | $12-25 | UART interface |
| 1 | GPS Antenna | 25×25mm ceramic patch | ~$5 | With U.FL |
| 1 | Meshtastic module | Heltec V3 | ~$20 | No built-in GPS |

### Option 3: PCB Integration

| Qty | Component | Part Number | Price | Notes |
|-----|-----------|-------------|-------|-------|
| 1 | GPS Module | u-blox MAX-M10S | ~$15 | SMD module |
| 1 | GPS LNA | SKY65313 | ~$2 | Optional, for active ant |
| 1 | U.FL connector | Hirose U.FL-R-SMT | ~$0.50 | GPS antenna |
| 1 | SAW filter | SF2049E | ~$1 | GPS band filter |
| 1 | Backup battery | ML621 | ~$2 | For fast warm start |

## Meshtastic Configuration

Configure the Meshtastic module for optimal GPS operation:

```bash
# Enable GPS
meshtastic --set position.gps_mode ENABLED

# Set smart position broadcast (only when moved significantly)
meshtastic --set position.position_broadcast_smart_enabled true

# Minimum distance for update (meters)
meshtastic --set position.broadcast_smart_minimum_distance 50

# Minimum interval (seconds)
meshtastic --set position.broadcast_smart_minimum_interval_secs 30

# Include altitude in position reports
meshtastic --set position.gps_en_3d_fix true

# Set position precision (32 = ~1m, 16 = ~100m)
meshtastic --set position.position_precision 32
```

## Software Integration

The position data is received via Meshtastic protobuf messages:

```protobuf
message Position {
  sfixed32 latitude_i = 1;   // Degrees * 1e7
  sfixed32 longitude_i = 2;  // Degrees * 1e7
  int32 altitude = 3;        // Meters above sea level
  uint32 time = 4;           // Unix timestamp
  uint32 PDOP = 5;           // Position dilution of precision * 100
  uint32 ground_speed = 6;   // m/s * 100
  uint32 ground_track = 7;   // Degrees * 100
  int32 sats_in_view = 8;    // Number of satellites
}
```

Parse in MeshtasticService and expose via API:
```
GET /api/location
GET /api/location/history?minutes=30
```

## Testing

### GPS Signal Quality Check

```bash
# View GPS info via Meshtastic CLI
meshtastic --port /dev/ttyAMA0 --get position

# Check satellite count and fix quality
# Good: 8+ satellites, PDOP < 2.0
# Acceptable: 4+ satellites, PDOP < 5.0
# Poor: <4 satellites, no fix
```

### Location Accuracy Test

1. Stand in open area with clear sky view
2. Compare reported position to known coordinates
3. Expected accuracy: 2-5 meters CEP (circular error probable)

### Indoor Performance Test

1. Test at various indoor locations
2. Note time-to-first-fix and satellite count
3. Document areas with no GPS coverage

## Privacy and Security

- Location data transmitted only over encrypted Meshtastic mesh
- Position stored locally on device, not uploaded to cloud
- Configurable tracking enable/disable
- History retention configurable (default: 24 hours)
- No external location services required
