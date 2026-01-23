# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Familiar Firmware is a .NET 8 ASP.NET Core application for Raspberry Pi that enables two-way audio communication between cosplay handlers (via phone) and cosplayers (via Pi device). It uses WiFi for primary audio streaming and Meshtastic LoRa mesh networking as backup with text-to-speech.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run the main application
dotnet run --project src/Familiar.Host

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Familiar.Audio.Tests

# Run benchmarks
dotnet run --project tests/Familiar.Benchmarks -c Release

# Publish for deployment
dotnet publish src/Familiar.Host -c Release -o /opt/familiar
```

## Architecture

### Project Structure

- **Familiar.Host** - ASP.NET Core web server (port 8080), entry point in `Program.cs`
- **Familiar.Audio** - ALSA audio playback/capture, Voice Activity Detection (VAD)
- **Familiar.Tts** - Text-to-speech via espeak subprocess
- **Familiar.Meshtastic** - LoRa mesh networking with protobuf protocol over serial
- **Familiar.Camera** - Pi Camera Module 3 support (Pi 5 only)

### Communication Flow

```
Handler's Phone ──WiFi──► Raspberry Pi
  │                           │
  ├─ /ws/audio/down ──────────┼──► Speaker (handler→cosplayer audio)
  ├─ /ws/audio/up ◄───────────┼─── Microphone (cosplayer→handler audio)
  └─ /ws/video ◄──────────────┼─── Camera stream (Pi 5 only)
                              │
Meshtastic App ──LoRa──► Pi ──┴──► TTS → Speaker (backup text messages)
```

### Key Patterns

- **Dependency Injection**: All services registered in `Program.cs`
- **IHostedService**: `MeshtasticService` runs as background service
- **Channels**: `System.Threading.Channels` for producer-consumer audio/message queues
- **Options Pattern**: Configuration via `IOptions<T>` from `appsettings.json`

## Testing

- Framework: xUnit with FluentAssertions and Moq
- Test projects mirror src structure: `tests/Familiar.Audio.Tests/`, etc.
- Performance benchmarks use BenchmarkDotNet

## Hardware Context

- Target: Raspberry Pi 4 (DIY) or Pi 5 (commercial with camera)
- Audio: ALSA via `aplay`/`arecord` subprocess or P/Invoke
- LoRa: Serial port communication with Meshtastic-flashed modules
- Camera: libcamera tools (Pi 5 only)

## AI Tool Usage Policy

From CONTRIBUTING.md:
- AI-assisted code is allowed with disclosure in PRs
- All AI-generated code must be reviewed by humans
- All tests must pass and build must succeed before merge
- Contributors must understand and be able to explain AI-generated code
- No AI for art assets
