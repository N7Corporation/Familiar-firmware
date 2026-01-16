# Familiar Firmware

A Raspberry Pi-based communication system for cosplay handlers to communicate with cosplayers via audio, featuring WiFi-based real-time voice communication and Meshtastic LoRa mesh network backup with text-to-speech.

## Overview

Familiar Firmware enables seamless one-way audio communication from a handler's phone to a speaker/earpiece worn by a cosplayer. This is particularly useful for:

- Guiding cosplayers through crowded convention spaces
- Providing cues during performances or photo ops
- Alerting cosplayers to schedule changes or emergencies
- Communicating without breaking character or removing costume elements

## Features

### Primary Communication (WiFi)
- Real-time audio streaming from handler's phone to cosplayer's device
- Web-based interface accessible from any smartphone browser
- Low-latency audio transmission over local WiFi
- No app installation required for the handler

### Backup Communication (Meshtastic)
- LoRa-based mesh networking for extended range
- Text messages converted to speech via TTS engine
- Works when WiFi is unavailable or out of range
- Integrates with existing Meshtastic network/devices

## Hardware Requirements

- Raspberry Pi (3B+, 4, or Zero 2 W recommended)
- LoRa radio module compatible with Meshtastic (e.g., SX1262, SX1276)
- Speaker or audio output device (3.5mm jack, I2S DAC, or Bluetooth)
- Power supply (battery pack for portability)
- MicroSD card (16GB+ recommended)
- Optional: Enclosure for costume integration

## Software Requirements

- Raspberry Pi OS (64-bit recommended)
- .NET 8.0 Runtime
- ASP.NET Core
- Meshtastic serial communication libraries
- ALSA audio libraries

## Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/familiar-firmware.git
cd familiar-firmware

# Run the setup script
./scripts/setup.sh

# Build the project
dotnet build

# Start the service
dotnet run --project src/Familiar.Host
```

## Usage

### Handler (Phone)
1. Connect to the Familiar WiFi network or same local network
2. Open the web interface in your browser
3. Grant microphone permissions
4. Press and hold the talk button to transmit audio

### Meshtastic Backup
1. Send a text message via Meshtastic app to the device's node
2. Message will be spoken aloud through the cosplayer's speaker

## Project Structure

```
familiar-firmware/
├── src/
│   ├── Familiar.Host/         # Main application host
│   ├── Familiar.Audio/        # Audio processing and playback
│   ├── Familiar.Web/          # Web interface and API
│   ├── Familiar.Meshtastic/   # Meshtastic integration
│   └── Familiar.Tts/          # Text-to-speech engine
├── web/                       # Frontend assets (HTML/JS/CSS)
├── config/                    # Configuration files
├── scripts/                   # Setup and utility scripts
├── tests/                     # Unit and integration tests
└── docs/                      # Additional documentation
```

## Configuration

Edit `config/appsettings.json` to customize:

```json
{
  "Familiar": {
    "WiFi": {
      "ApMode": true,
      "Ssid": "Familiar-AP",
      "Password": "your-password"
    },
    "Audio": {
      "OutputDevice": "default",
      "SampleRate": 48000,
      "BufferSize": 1024
    },
    "Meshtastic": {
      "Port": "/dev/ttyUSB0",
      "BaudRate": 115200,
      "NodeName": "Familiar"
    },
    "Tts": {
      "Engine": "espeak",
      "Voice": "en",
      "Rate": 150
    }
  }
}
```

## Documentation

- [ROADMAP.md](ROADMAP.md) - Development phases and milestones
- [TECHNICAL.md](TECHNICAL.md) - Architecture and implementation details

## License

MIT License - See LICENSE file for details.

## Contributing

Contributions are welcome! Please read CONTRIBUTING.md for guidelines.

## Acknowledgments

- [Meshtastic](https://meshtastic.org/) - LoRa mesh networking
- The cosplay community for inspiration
