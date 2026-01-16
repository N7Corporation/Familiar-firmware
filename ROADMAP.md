# Familiar Firmware Roadmap

## Phase 1: Foundation
**Goal**: Basic hardware setup and proof of concept

### Milestone 1.1: Hardware Assembly
- [ ] Select and acquire Raspberry Pi model
- [ ] Select and acquire compatible LoRa module
- [ ] Assemble hardware components
- [ ] Verify basic connectivity and power requirements
- [ ] Document hardware setup process

### Milestone 1.2: Base System Setup
- [ ] Install Raspberry Pi OS (64-bit)
- [ ] Configure WiFi (both client and AP modes)
- [ ] Install .NET 8.0 Runtime
- [ ] Install ALSA audio libraries
- [ ] Test audio output (speakers/3.5mm/I2S)
- [ ] Create basic startup scripts

### Milestone 1.3: Meshtastic Integration
- [ ] Flash LoRa module with Meshtastic firmware
- [ ] Test serial communication with LoRa module
- [ ] Send and receive test messages via CLI
- [ ] Document Meshtastic configuration

---

## Phase 2: Core Features
**Goal**: Implement primary functionality with .NET

### Milestone 2.1: Project Setup
- [ ] Create .NET 8 solution structure
- [ ] Set up Familiar.Host project (ASP.NET Core)
- [ ] Set up Familiar.Audio class library
- [ ] Set up Familiar.Meshtastic class library
- [ ] Set up Familiar.Tts class library
- [ ] Configure dependency injection

### Milestone 2.2: Text-to-Speech System
- [ ] Evaluate TTS options (espeak via Process, Azure Speech, local engines)
- [ ] Implement ITtsEngine interface
- [ ] Create EspeakTtsEngine implementation
- [ ] Add voice selection and configuration
- [ ] Add speech rate and volume controls
- [ ] Test TTS output quality

### Milestone 2.3: Meshtastic Message Handling
- [ ] Implement serial port communication (System.IO.Ports)
- [ ] Parse Meshtastic protobuf messages
- [ ] Create MeshtasticService as hosted service
- [ ] Implement message queue with Channel<T>
- [ ] Connect incoming messages to TTS
- [ ] Add message filtering (node ID, channel)
- [ ] Handle special commands/prefixes

### Milestone 2.4: Web Server Foundation
- [ ] Set up ASP.NET Core minimal API
- [ ] Create basic REST endpoints
- [ ] Implement device status endpoint
- [ ] Add configuration API with IOptions<T>
- [ ] Create systemd service for auto-start

---

## Phase 3: Audio Streaming
**Goal**: Real-time voice communication over WiFi

### Milestone 3.1: Audio Protocol Selection
- [ ] Evaluate WebSocket vs SignalR for audio streaming
- [ ] Prototype WebSocket binary streaming
- [ ] Measure latency and quality
- [ ] Select optimal solution
- [ ] Document protocol decision

### Milestone 3.2: Server-Side Audio
- [ ] Implement WebSocket endpoint for audio
- [ ] Create audio buffer management with System.Threading.Channels
- [ ] Integrate with ALSA via P/Invoke or native library
- [ ] Handle connection drops gracefully
- [ ] Optimize for low latency

### Milestone 3.3: Web Interface (Handler)
- [ ] Create responsive mobile-first UI
- [ ] Implement microphone capture with Web Audio API
- [ ] Add push-to-talk button
- [ ] Show connection status
- [ ] Add volume/settings controls
- [ ] Bundle static files with ASP.NET Core

---

## Phase 4: Polish & Reliability
**Goal**: Production-ready system

### Milestone 4.1: Robustness
- [ ] Add automatic reconnection logic
- [ ] Implement IHostedService lifecycle management
- [ ] Add structured logging with Serilog
- [ ] Handle edge cases (no audio device, etc.)
- [ ] Create health check endpoint
- [ ] Add Polly for retry policies

### Milestone 4.2: Security
- [ ] Add JWT authentication to web interface
- [ ] Implement HTTPS with Kestrel
- [ ] Secure Meshtastic channel encryption
- [ ] Add rate limiting middleware
- [ ] Security audit

### Milestone 4.3: User Experience
- [ ] Add audio feedback tones (connected, etc.)
- [ ] Implement volume normalization
- [ ] Add noise suppression option
- [ ] Create setup wizard
- [ ] Improve error messages

---

## Phase 5: Extended Features
**Goal**: Enhanced functionality

### Milestone 5.1: Mobile App (Optional)
- [ ] Evaluate .NET MAUI for cross-platform app
- [ ] Create app with persistent connection
- [ ] Add background operation
- [ ] Implement push notifications
- [ ] App store deployment

### Milestone 5.2: Multi-Device Support
- [ ] Support multiple handlers
- [ ] Support multiple cosplayers
- [ ] Implement device pairing
- [ ] Add group messaging
- [ ] Create device management UI

### Milestone 5.3: Advanced Audio
- [ ] Add Bluetooth audio output support
- [ ] Implement audio mixing (TTS + stream)
- [ ] Add priority system (urgent messages)
- [ ] Voice activity detection
- [ ] Echo cancellation (for two-way future)

---

## Phase 6: Hardware Integration
**Goal**: Costume-ready device

### Milestone 6.1: Form Factor
- [ ] Design compact enclosure
- [ ] Create 3D printable case
- [ ] Optimize for battery operation
- [ ] Add status LEDs (GPIO control)
- [ ] Document assembly

### Milestone 6.2: Power Management
- [ ] Measure power consumption
- [ ] Implement power saving modes
- [ ] Add battery monitoring
- [ ] Low battery warnings
- [ ] Optimize for full-day operation

### Milestone 6.3: Costume Integration Guide
- [ ] Document hiding techniques
- [ ] Speaker placement recommendations
- [ ] Wiring best practices
- [ ] Heat management
- [ ] Quick-disconnect options

---

## Future Considerations

### Potential Features
- Two-way audio communication
- Pre-recorded message buttons
- Integration with other prop electronics
- GPS location sharing via Meshtastic
- Convention schedule integration
- Multiple language TTS support

### Hardware Variants
- ESP32-based lightweight version
- Raspberry Pi Pico W option
- Commercial off-the-shelf integration

### .NET Ecosystem Opportunities
- Blazor WebAssembly for richer web UI
- .NET MAUI Blazor Hybrid for mobile app
- gRPC for efficient binary communication
- Native AOT compilation for faster startup

---

## Version Milestones

| Version | Status | Description |
|---------|--------|-------------|
| 0.1.0 | Planned | Basic TTS via Meshtastic |
| 0.2.0 | Planned | Web interface foundation |
| 0.3.0 | Planned | Audio streaming MVP |
| 0.4.0 | Planned | Reliability improvements |
| 1.0.0 | Planned | Production-ready release |
| 1.1.0 | Planned | Mobile app release |
| 2.0.0 | Future | Multi-device support |
