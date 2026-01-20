---
name: Hardware Test Report
about: Report results from testing on hardware
title: '[TEST] '
labels: testing, hardware
assignees: ''
---

## Test Environment

### Hardware
- **Pi Model**: (e.g., Raspberry Pi 4 Model B 4GB)
- **Audio Output**: (e.g., USB speaker, 3.5mm jack, I2S DAC)
- **Audio Input**: (e.g., USB mic, I2S MEMS mic)
- **Meshtastic Device**: (e.g., T-Beam v1.1, Heltec V3)
- **Camera** (Pi 5 only): (e.g., Pi Camera Module 3)

### Software
- **OS**: (e.g., Raspberry Pi OS Bookworm 64-bit)
- **.NET Version**:
- **Familiar Commit**:

## Test Results

### Audio Playback
- [ ] TTS plays through speaker
- [ ] WebSocket audio from handler plays
- [ ] Volume control works
- [ ] Mute/unmute works

**Notes**:

### Audio Capture (if mic connected)
- [ ] Mic captures audio
- [ ] VOX detection triggers
- [ ] PTT mode works
- [ ] Audio streams to handler

**Notes**:

### Meshtastic
- [ ] Connects to device
- [ ] Receives messages
- [ ] Sends messages
- [ ] TTS speaks received messages
- [ ] Commands (!vol, !mute) work

**Notes**:

### Camera (Pi 5 only)
- [ ] Camera detected
- [ ] Snapshot works
- [ ] Live stream works
- [ ] Recording works

**Notes**:

### Web Interface
- [ ] Loads on phone browser
- [ ] PTT button works
- [ ] Quick messages work
- [ ] Status displays correctly

**Notes**:

### WiFi AP Mode
- [ ] AP broadcasts SSID
- [ ] Phone connects to AP
- [ ] Web interface accessible at 192.168.4.1:8080

**Notes**:

## Issues Found
List any issues discovered during testing (link to bug reports if created).

## Performance Notes
- CPU usage:
- Memory usage:
- Audio latency (estimated):
- Video latency (estimated):

## Additional Observations
Any other notes from testing.
