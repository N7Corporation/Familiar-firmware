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
- [ ] Configure WiFi Access Point (hostapd + dnsmasq)
- [ ] Install .NET 8.0 Runtime
- [ ] Install ALSA audio libraries and espeak
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
- [x] Create .NET 8 solution structure
- [x] Set up Familiar.Host project (ASP.NET Core)
- [x] Set up Familiar.Audio class library
- [x] Set up Familiar.Meshtastic class library
- [x] Set up Familiar.Tts class library
- [x] Configure dependency injection

### Milestone 2.2: Text-to-Speech System
- [x] Evaluate TTS options (espeak via Process, Azure Speech, local engines)
- [x] Implement ITtsEngine interface
- [x] Create EspeakTtsEngine implementation
- [x] Add voice selection and configuration
- [x] Add speech rate and volume controls
- [x] Test TTS output quality

### Milestone 2.3: Meshtastic Message Handling
- [x] Implement serial port communication (System.IO.Ports)
- [x] Parse Meshtastic protobuf messages
- [x] Create MeshtasticService as hosted service
- [x] Implement message queue with Channel<T>
- [x] Connect incoming messages to TTS
- [x] Add message filtering (node ID, channel)
- [x] Handle special commands/prefixes

### Milestone 2.4: Web Server Foundation
- [x] Set up ASP.NET Core minimal API
- [x] Create basic REST endpoints
- [x] Implement device status endpoint
- [x] Add configuration API with IOptions<T>
- [x] Create systemd service for auto-start

---

## Phase 3: Audio Streaming
**Goal**: Real-time voice communication over WiFi

### Milestone 3.1: Audio Protocol Selection
- [x] Evaluate WebSocket vs SignalR for audio streaming
- [x] Prototype WebSocket binary streaming
- [x] Measure latency and quality
- [x] Select optimal solution
- [x] Document protocol decision

### Milestone 3.2: Server-Side Audio (Handler → Cosplayer)
- [x] Implement WebSocket endpoint for receiving handler audio
- [x] Create audio buffer management with System.Threading.Channels
- [x] Integrate with ALSA playback via aplay/P/Invoke
- [x] Handle connection drops gracefully
- [x] Optimize for low latency

### Milestone 3.3: Cosplayer Microphone (Cosplayer → Handler)
- [x] Implement ALSA audio capture via arecord
- [x] Create WebSocket endpoint for streaming to handler
- [x] Implement Voice Activity Detection (VAD)
- [x] Add VOX mode (voice-activated transmission)
- [x] Add PTT mode (GPIO button support)
- [x] Handle echo/feedback prevention

### Milestone 3.4: Web Interface (Handler)
- [x] Create responsive mobile-first UI
- [x] Implement microphone capture with Web Audio API
- [x] Add push-to-talk button for handler
- [x] Add audio playback for cosplayer's voice
- [x] Show connection status and voice activity indicators
- [x] Add volume/settings controls
- [x] Bundle static files with ASP.NET Core

---

## Phase 4: Polish & Reliability
**Goal**: Production-ready system

### Milestone 4.1: Robustness
- [x] Add automatic reconnection logic
- [x] Implement IHostedService lifecycle management
- [x] Add structured logging with Serilog
- [x] Handle edge cases (no audio device, etc.)
- [x] Create health check endpoint
- [ ] Add Polly for retry policies

### Milestone 4.2: Security
- [x] Add JWT authentication to web interface
- [ ] Implement HTTPS with Kestrel
- [ ] Secure Meshtastic channel encryption
- [x] Add rate limiting middleware
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
- [ ] Improved VAD with noise floor calibration
- [ ] Echo cancellation for full-duplex audio
- [ ] Noise suppression on cosplayer mic

---

## Phase 6: Hardware Integration (DIY - Pi 4)
**Goal**: Costume-ready DIY device

### Milestone 6.1: Form Factor
- [ ] Design compact enclosure for Pi 4
- [ ] Create 3D printable case
- [ ] Optimize for battery operation
- [ ] Add status LEDs (GPIO control)
- [ ] Document assembly with off-the-shelf parts

### Milestone 6.2: Power Management
- [ ] Measure power consumption (Pi 4 baseline)
- [ ] Implement power saving modes
- [ ] Add battery monitoring via ADC
- [ ] Low battery warnings (audio + LED)
- [ ] Target 8+ hours on 10000mAh

### Milestone 6.3: Costume Integration Guide
- [ ] Document hiding techniques
- [ ] Speaker placement recommendations
- [ ] Wiring best practices
- [ ] Heat management
- [ ] Quick-disconnect options

---

## Phase 7: Commercial Version (Pi 5)
**Goal**: Production-ready commercial hardware

### Milestone 7.1: Pi 5 Optimization
- [ ] Test and optimize for Pi 5 performance
- [ ] Leverage improved CPU for lower latency
- [ ] Utilize Pi 5 power management features
- [ ] Test with 27W USB-C PD power delivery
- [ ] Enable hardware video encoding (H.264/HEVC)

### Milestone 7.2: Custom PCB Design
- [ ] Design carrier board for Pi 5 CM (Compute Module) or full Pi 5
- [ ] Integrate I2S DAC + Class D amplifier
- [ ] Integrate LoRa module (SX1262)
- [ ] Add battery charging circuit (BMS)
- [ ] Add fuel gauge IC for accurate battery %
- [ ] Status LEDs and tactile buttons

### Milestone 7.3: Pi Camera Integration
- [ ] Integrate Pi Camera Module 3 support
- [ ] Implement live video streaming (WebRTC/HLS)
- [ ] Add recording to local storage (H.264/HEVC)
- [ ] Snapshot capture API endpoint
- [ ] Picture-in-picture view in handler web UI
- [ ] Camera settings (resolution, framerate, exposure)
- [ ] Optional: Wide-angle or fisheye lens support

### Milestone 7.4: Commercial Enclosure
- [ ] Design injection-moldable enclosure
- [ ] Optimize thermal design for Pi 5
- [ ] IP rating for sweat/light moisture resistance
- [ ] Belt clip and lanyard attachment points
- [ ] Speaker grille and microphone port
- [ ] Camera lens window/port
- [ ] Flexible camera cable routing

### Milestone 7.5: Manufacturing Prep
- [ ] BOM optimization for cost
- [ ] Assembly documentation
- [ ] QA test procedures
- [ ] FCC/CE certification considerations
- [ ] Packaging design

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
- **DIY Kit**: Pi 4 + off-the-shelf components
- **Commercial Unit**: Pi 5 + custom PCB + integrated enclosure

### .NET Ecosystem Opportunities
- Blazor WebAssembly for richer web UI
- .NET MAUI Blazor Hybrid for mobile app
- gRPC for efficient binary communication
- Native AOT compilation for faster startup

---

## Version Milestones

| Version | Status | Target Hardware | Description |
|---------|--------|-----------------|-------------|
| 0.1.0 | Complete | Pi 4 | Basic TTS via Meshtastic |
| 0.2.0 | Complete | Pi 4 | Web interface foundation |
| 0.3.0 | Complete | Pi 4 | Audio streaming MVP |
| 0.4.0 | In Progress | Pi 4 | Reliability improvements |
| 1.0.0 | Planned | Pi 4 | DIY release - fully functional |
| 1.1.0 | Planned | Pi 4 | Mobile app (optional) |
| 2.0.0 | Planned | Pi 4 / Pi 5 | Multi-device support |
| 3.0.0 | Future | Pi 5 | Commercial hardware release |
