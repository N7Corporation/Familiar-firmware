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
- **Two-way audio** between handler and cosplayer
- Handler speaks into phone → cosplayer hears through earpiece
- Cosplayer speaks into device mic → handler hears on phone
- Web-based interface accessible from any smartphone browser
- Low-latency audio transmission over local WiFi
- Push-to-talk or voice-activated modes

### Backup Communication (Meshtastic)
- LoRa-based mesh networking for extended range
- Text messages converted to speech via TTS engine
- Works when WiFi is unavailable or out of range
- Integrates with existing Meshtastic network/devices

### Camera Features (Pi 5 Commercial Only)
- Live video streaming to handler's phone
- POV recording for content creation
- Snapshot capture on demand
- Handler can see what the cosplayer sees

## Hardware Requirements

### DIY Version (Minimum Spec)
- **Raspberry Pi 4** (2GB+ RAM) - minimum supported
- LoRa radio module compatible with Meshtastic (e.g., SX1262, SX1276)
- Speaker or audio output device (3.5mm jack, I2S DAC, or USB audio)
- **Microphone** for cosplayer voice (USB mic, I2S MEMS mic, or USB sound card with mic input)
- 5V 3A power supply / battery pack
- MicroSD card (16GB+ recommended)

### Commercial Version
- **Raspberry Pi 5** (4GB+ RAM) - better performance, lower latency
- Integrated LoRa module
- Custom PCB with I2S DAC + amplifier + **MEMS microphone**
- Optimized enclosure for costume integration
- Battery management system
- **Pi Camera Module 3** - live streaming & recording (Pi 5 only)

## Software Requirements

- Raspberry Pi OS (64-bit recommended)
- .NET 8.0 Runtime
- ASP.NET Core
- hostapd (WiFi access point)
- dnsmasq (DHCP server)
- ALSA audio libraries
- espeak (text-to-speech)

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
1. Connect to the **Familiar** WiFi network (hosted by the Pi)
2. Open `http://192.168.4.1` in your browser
3. Grant microphone permissions
4. Press and hold the talk button to transmit audio
5. Hear cosplayer's responses through your phone speaker

### Cosplayer (Device)
- Wears earpiece connected to the Pi
- Speaks into integrated or attached microphone
- Audio automatically streams to handler (voice-activated or PTT button)

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
