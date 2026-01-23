# Voice Chat Rooms Implementation Plan

## Executive Summary

This plan outlines the implementation of voice chat rooms for the Familiar Firmware project. The feature extends the existing 1:1 audio communication to support multi-user voice channels with server-side audio mixing, room management, and Meshtastic bridge integration.

## 1. Architecture Overview

### Component Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                           Familiar.Host                                   │
│  ┌────────────────────┐  ┌────────────────────┐  ┌──────────────────────┐│
│  │  VoiceRoomEndpoints│  │ VoiceWebSocketHandler│ │ MeshtasticBridgeService││
│  │  (REST API)        │  │ (/ws/voice)         │  │ (IHostedService)     ││
│  └─────────┬──────────┘  └─────────┬───────────┘  └──────────┬───────────┘│
│            │                       │                          │           │
│            └───────────────────────┼──────────────────────────┘           │
│                                    ▼                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      IVoiceRoomService                               │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐│ │
│  │  │                    VoiceRoomService                             ││ │
│  │  │  • Room management (CRUD)                                       ││ │
│  │  │  • User/connection tracking                                     ││ │
│  │  │  • Room state broadcasts                                        ││ │
│  │  └─────────────────────────────────────────────────────────────────┘│ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                    │                                      │
│                                    ▼                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      IAudioMixer                                     │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐│ │
│  │  │                    OpusAudioMixer                               ││ │
│  │  │  • Decode incoming Opus frames                                  ││ │
│  │  │  • Mix PCM from multiple sources                                ││ │
│  │  │  • Re-encode to Opus                                            ││ │
│  │  │  • Per-user selective mixing (exclude sender)                   ││ │
│  │  └─────────────────────────────────────────────────────────────────┘│ │
│  └─────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
User A sends audio
        │
        ▼
┌───────────────────┐
│  VoiceConnection  │──► Channel<AudioFrame> ──┐
│  (User A)         │                          │
└───────────────────┘                          │
                                               ▼
User B sends audio              ┌─────────────────────────┐
        │                       │    AudioMixerWorker     │
        ▼                       │  (per-room background)  │
┌───────────────────┐           │                         │
│  VoiceConnection  │──► ───────►  Mix frames for each   │
│  (User B)         │           │  recipient (excluding  │
└───────────────────┘           │  sender)               │
                                └────────────┬────────────┘
                                             │
                      ┌──────────────────────┴──────────────────────┐
                      ▼                                              ▼
           ┌───────────────────┐                         ┌───────────────────┐
           │  VoiceConnection  │                         │  VoiceConnection  │
           │  (User A) receives│                         │  (User B) receives│
           │  mixed B audio    │                         │  mixed A audio    │
           └───────────────────┘                         └───────────────────┘
```

---

## 2. New Files to Create

### 2.1 Core Domain Models (`src/Familiar.Host/VoiceRooms/Models/`)

| File | Purpose |
|------|---------|
| `VoiceRoom.cs` | Immutable record for room data (Id, Name, Type, MaxUsers, etc.) |
| `VoiceRoomUser.cs` | Record for user state (UserId, DisplayName, Role, IsMuted, IsSpeaking) |
| `VoiceRoomType.cs` | Enum: Direct, Team, Group, Broadcast |
| `UserRole.cs` | Enum: Handler, Cosplayer, Observer |
| `AudioFrame.cs` | Record for timestamped audio data with sequence number |

### 2.2 Configuration (`src/Familiar.Host/Options/`)

| File | Purpose |
|------|---------|
| `VoiceRoomOptions.cs` | Options class for VoiceRooms section (Enabled, MaxRooms, DefaultRoom, Codec, Bitrate, etc.) |
| `RoomConfiguration.cs` | Nested class for pre-configured room definitions |

### 2.3 Service Interfaces (`src/Familiar.Host/VoiceRooms/`)

| File | Purpose |
|------|---------|
| `IVoiceRoomService.cs` | Room management interface (Create, Delete, Get, List rooms) |
| `IVoiceRoomConnectionManager.cs` | Connection tracking interface (Join, Leave, GetConnections) |
| `IAudioMixer.cs` | Audio mixing interface (AddFrame, GetMixedFrame) |
| `IVoiceRoomBroadcaster.cs` | Room event broadcasting interface (UserJoined, UserLeft, StateChanged) |

### 2.4 Service Implementations (`src/Familiar.Host/VoiceRooms/Services/`)

| File | Purpose |
|------|---------|
| `VoiceRoomService.cs` | Implements `IVoiceRoomService` - manages room lifecycle |
| `VoiceRoomConnectionManager.cs` | Implements `IVoiceRoomConnectionManager` - tracks active connections |
| `OpusAudioMixer.cs` | Implements `IAudioMixer` - PCM mixing with Opus encode/decode |
| `VoiceRoomBroadcaster.cs` | Implements `IVoiceRoomBroadcaster` - sends events to room members |
| `AudioMixerWorker.cs` | Background worker that processes audio mixing per room |

### 2.5 Connection Management (`src/Familiar.Host/VoiceRooms/`)

| File | Purpose |
|------|---------|
| `VoiceRoomConnection.cs` | Represents a single user's WebSocket connection to a room |
| `VoiceConnectionState.cs` | Mutable state class (Muted, Deafened, Speaking, LastActivity) |

### 2.6 WebSocket Handler (`src/Familiar.Host/WebSockets/`)

| File | Purpose |
|------|---------|
| `VoiceWebSocketHandler.cs` | Handles `/ws/voice` connections - joins room, routes audio |

### 2.7 REST API Endpoints (`src/Familiar.Host/Endpoints/`)

| File | Purpose |
|------|---------|
| `VoiceRoomEndpoints.cs` | Maps `/api/voice/rooms/*` and `/api/voice/*` endpoints |

### 2.8 WebSocket Protocol (`src/Familiar.Host/VoiceRooms/Protocol/`)

| File | Purpose |
|------|---------|
| `VoiceMessage.cs` | Base class for WebSocket messages |
| `VoiceMessageTypes.cs` | Static class with message type constants |
| `JoinMessage.cs` | Join room request message |
| `UserJoinedMessage.cs` | User joined broadcast message |
| `AudioMessage.cs` | Audio frame message (JSON or binary header) |
| `PttMessage.cs` | Push-to-talk state message |
| `SpeakingMessage.cs` | Speaking indicator message |
| `RoomStateMessage.cs` | Full room state message |
| `VoiceMessageSerializer.cs` | Serialization helper for JSON/binary messages |

### 2.9 Meshtastic Bridge (`src/Familiar.Host/VoiceRooms/Services/`)

| File | Purpose |
|------|---------|
| `MeshtasticVoiceBridgeService.cs` | `IHostedService` that bridges voice rooms to Meshtastic |
| `IMeshtasticVoiceBridge.cs` | Interface for bridge operations |

### 2.10 DI Registration (`src/Familiar.Host/VoiceRooms/`)

| File | Purpose |
|------|---------|
| `ServiceCollectionExtensions.cs` | `AddVoiceRooms()` extension method for DI registration |

### 2.11 Request/Response DTOs (`src/Familiar.Host/VoiceRooms/Contracts/`)

| File | Purpose |
|------|---------|
| `CreateRoomRequest.cs` | POST /api/voice/rooms request body |
| `JoinRoomRequest.cs` | POST /api/voice/rooms/{id}/join request body |
| `RoomResponse.cs` | Room details response |
| `RoomListResponse.cs` | List of rooms response |
| `UserSettingsRequest.cs` | Mute/unmute/deafen request |

### 2.12 Tests (`tests/Familiar.Host.Tests/VoiceRooms/`)

| File | Purpose |
|------|---------|
| `VoiceRoomServiceTests.cs` | Unit tests for room management |
| `VoiceRoomConnectionManagerTests.cs` | Unit tests for connection tracking |
| `OpusAudioMixerTests.cs` | Unit tests for audio mixing logic |
| `VoiceWebSocketHandlerTests.cs` | Integration tests for WebSocket handler |
| `VoiceRoomEndpointsTests.cs` | Integration tests for REST endpoints |

### 2.13 Web UI (`src/Familiar.Host/wwwroot/`)

| File | Purpose |
|------|---------|
| `voice-rooms.js` | JavaScript module for voice room functionality |
| Update `index.html` | Add voice rooms section |
| Update `style.css` | Add voice room styles |

---

## 3. Existing Files to Modify

| File | Modifications |
|------|---------------|
| `src/Familiar.Host/Program.cs` | Add `builder.Services.Configure<VoiceRoomOptions>()`, `builder.Services.AddVoiceRooms()`, map `/ws/voice` endpoint, call `app.MapVoiceRoomEndpoints()` |
| `src/Familiar.Host/appsettings.json` | Add `VoiceRooms` configuration section |
| `src/Familiar.Host/wwwroot/index.html` | Add voice rooms UI section |
| `src/Familiar.Host/wwwroot/style.css` | Add voice room styling |
| `src/Familiar.Host/wwwroot/app.js` | Import and integrate voice rooms module |

---

## 4. Implementation Order

### Phase 1: Core Infrastructure (Foundation)

**Step 1.1: Configuration and Options**
1. Create `VoiceRoomOptions.cs` with configuration properties
2. Create `RoomConfiguration.cs` for pre-defined rooms
3. Update `appsettings.json` with VoiceRooms section

**Step 1.2: Domain Models**
1. Create `UserRole.cs` enum
2. Create `VoiceRoomType.cs` enum
3. Create `VoiceRoomUser.cs` record
4. Create `VoiceRoom.cs` record
5. Create `AudioFrame.cs` record

**Step 1.3: Service Interfaces**
1. Create `IVoiceRoomService.cs`
2. Create `IVoiceRoomConnectionManager.cs`
3. Create `IAudioMixer.cs`
4. Create `IVoiceRoomBroadcaster.cs`

### Phase 2: Room Management

**Step 2.1: Room Service**
1. Create `VoiceRoomService.cs` implementing `IVoiceRoomService`
   - In-memory room storage with `ConcurrentDictionary`
   - Room CRUD operations
   - Password validation
   - User limit enforcement

**Step 2.2: Connection Manager**
1. Create `VoiceRoomConnection.cs` class
2. Create `VoiceConnectionState.cs` class
3. Create `VoiceRoomConnectionManager.cs` implementing `IVoiceRoomConnectionManager`
   - Track connections per room
   - Handle user join/leave
   - Manage connection state

**Step 2.3: REST API Endpoints**
1. Create request/response DTOs in `Contracts/`
2. Create `VoiceRoomEndpoints.cs` with all REST endpoints
3. Update `Program.cs` to map endpoints

**Step 2.4: Unit Tests for Room Management**
1. Create `VoiceRoomServiceTests.cs`
2. Create `VoiceRoomConnectionManagerTests.cs`

### Phase 3: WebSocket Communication

**Step 3.1: Protocol Messages**
1. Create all message classes in `Protocol/`
2. Create `VoiceMessageSerializer.cs`

**Step 3.2: Broadcaster**
1. Create `VoiceRoomBroadcaster.cs` implementing `IVoiceRoomBroadcaster`
   - Broadcast to all room members
   - Broadcast excluding specific users

**Step 3.3: WebSocket Handler**
1. Create `VoiceWebSocketHandler.cs`
   - Handle WebSocket connection lifecycle
   - Parse incoming messages
   - Route audio to mixer
   - Send room state updates
2. Update `Program.cs` to register handler and map `/ws/voice`

**Step 3.4: Integration Tests**
1. Create `VoiceWebSocketHandlerTests.cs`

### Phase 4: Audio Mixing

**Step 4.1: Audio Mixer Core**
1. Create `OpusAudioMixer.cs` implementing `IAudioMixer`
   - Use `Concentus` NuGet package for Opus codec (pure C# implementation)
   - Decode Opus to PCM
   - Mix PCM samples with clipping prevention
   - Re-encode to Opus

**Step 4.2: Mixer Worker**
1. Create `AudioMixerWorker.cs`
   - Background task per active room
   - Collect frames from all speaking users
   - Mix and distribute to recipients
   - Use `System.Threading.Channels` for audio queues

**Step 4.3: Audio Mixer Tests**
1. Create `OpusAudioMixerTests.cs`

### Phase 5: DI Registration and Integration

**Step 5.1: Service Registration**
1. Create `ServiceCollectionExtensions.cs` with `AddVoiceRooms()` method
2. Update `Program.cs` to call `AddVoiceRooms()`

**Step 5.2: End-to-End Integration**
1. Wire up all components in `Program.cs`
2. Initialize persistent rooms from configuration
3. Test complete flow

### Phase 6: Meshtastic Bridge (Optional Enhancement)

**Step 6.1: Bridge Interface**
1. Create `IMeshtasticVoiceBridge.cs`

**Step 6.2: Bridge Service**
1. Create `MeshtasticVoiceBridgeService.cs` as `IHostedService`
   - Listen for room audio
   - Encode to low-bitrate format for LoRa
   - Relay text-to-speech for incoming Meshtastic messages

### Phase 7: Web UI

**Step 7.1: JavaScript Module**
1. Create `voice-rooms.js` with:
   - VoiceRoomClient class
   - Room list management
   - Voice connection handling
   - UI state management

**Step 7.2: HTML Updates**
1. Add voice rooms section to `index.html`
   - Room list with member display
   - Voice controls (PTT, mute, deafen)
   - Create room modal

**Step 7.3: CSS Styling**
1. Add voice room styles to `style.css`
   - Room list styling
   - Speaking indicators
   - Voice control buttons

### Phase 8: Security Hardening

**Step 8.1: Access Control**
1. Add room password validation
2. Implement kick/ban functionality
3. Add room admin role support

**Step 8.2: Rate Limiting**
1. Add audio transmission rate limiting
2. Add room creation rate limiting

---

## 5. Key Interface Definitions

### IVoiceRoomService

```csharp
public interface IVoiceRoomService
{
    IReadOnlyList<VoiceRoom> Rooms { get; }

    Task<VoiceRoom> CreateRoomAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<bool> DeleteRoomAsync(string roomId, CancellationToken ct = default);
    Task<VoiceRoom?> GetRoomAsync(string roomId, CancellationToken ct = default);
    Task<IReadOnlyList<VoiceRoom>> GetRoomsAsync(CancellationToken ct = default);
    bool ValidatePassword(string roomId, string? password);
}
```

### IVoiceRoomConnectionManager

```csharp
public interface IVoiceRoomConnectionManager
{
    Task<VoiceRoomConnection> JoinRoomAsync(
        string roomId,
        string userId,
        string displayName,
        UserRole role,
        WebSocket webSocket,
        CancellationToken ct = default);

    Task LeaveRoomAsync(string roomId, string userId, CancellationToken ct = default);

    IReadOnlyList<VoiceRoomConnection> GetRoomConnections(string roomId);
    VoiceRoomConnection? GetConnection(string roomId, string userId);

    Task UpdateStateAsync(string roomId, string userId, Action<VoiceConnectionState> update);
}
```

### IAudioMixer

```csharp
public interface IAudioMixer
{
    void AddFrame(string roomId, string userId, ReadOnlyMemory<byte> opusFrame, long sequence);

    ReadOnlyMemory<byte> GetMixedFrame(
        string roomId,
        string excludeUserId,
        out IReadOnlyList<string> contributors);

    void RemoveUser(string roomId, string userId);
    void RemoveRoom(string roomId);
}
```

### IVoiceRoomBroadcaster

```csharp
public interface IVoiceRoomBroadcaster
{
    Task BroadcastToRoomAsync<T>(string roomId, T message, CancellationToken ct = default) where T : class;
    Task BroadcastToRoomExceptAsync<T>(string roomId, string excludeUserId, T message, CancellationToken ct = default) where T : class;
    Task SendToUserAsync<T>(string roomId, string userId, T message, CancellationToken ct = default) where T : class;
}
```

---

## 6. Configuration Structure

```json
{
  "Familiar": {
    "VoiceRooms": {
      "Enabled": true,
      "MaxRooms": 10,
      "MaxUsersPerRoom": 20,
      "DefaultRoom": "main",
      "Codec": "opus",
      "Bitrate": 32000,
      "FrameDurationMs": 20,
      "EnableMeshtasticBridge": true,
      "MixingBufferMs": 60,
      "Rooms": [
        {
          "Id": "main",
          "Name": "Main Channel",
          "Type": "Group",
          "Password": null,
          "MaxUsers": 20,
          "Persistent": true
        },
        {
          "Id": "vip",
          "Name": "VIP Room",
          "Type": "Team",
          "Password": "secret123",
          "MaxUsers": 5,
          "Persistent": true
        }
      ]
    }
  }
}
```

---

## 7. Dependencies

### NuGet Packages to Add

| Package | Purpose | Version |
|---------|---------|---------|
| `Concentus` | Pure C# Opus codec for audio encoding/decoding | 2.x |
| `Concentus.OggFile` | Optional: Ogg container support | 2.x |

### Existing Dependencies to Leverage

- `System.Threading.Channels` - Already used for audio queues
- `System.Text.Json` - Already used for WebSocket JSON messages
- `Microsoft.Extensions.Options` - Already used for configuration
- `Microsoft.Extensions.Hosting` - Already used for background services

---

## 8. Key Architectural Decisions

### 8.1 Server-Side Audio Mixing
**Decision**: Mix audio on the server rather than client-side.
**Rationale**:
- Reduces bandwidth for clients (receive one mixed stream vs. N streams)
- Simplifies client implementation
- Better for resource-constrained clients (mobile phones)
- Consistent with existing audio architecture

### 8.2 Opus Codec
**Decision**: Use Opus codec for all voice data.
**Rationale**:
- Industry standard for real-time voice
- Excellent quality at low bitrates (32 kbps)
- Low latency
- Pure C# implementation available (Concentus)

### 8.3 Channel-Based Audio Queues
**Decision**: Use `System.Threading.Channels` for audio frame routing.
**Rationale**:
- Consistent with existing `AudioManager` pattern
- High-performance bounded channels prevent memory issues
- Natural backpressure handling with `BoundedChannelFullMode.DropOldest`

### 8.4 Per-Room Mixer Workers
**Decision**: One background task per active room for mixing.
**Rationale**:
- Isolates room processing
- Scales with room count
- Workers can be started/stopped as rooms become active/inactive

### 8.5 Singleton Services
**Decision**: Register `VoiceRoomService` and `VoiceRoomConnectionManager` as singletons.
**Rationale**:
- Consistent with existing `AudioManager` pattern
- Room state must persist across requests
- Connection tracking requires shared state

---

## 9. Potential Challenges and Mitigations

| Challenge | Mitigation |
|-----------|------------|
| Audio latency from mixing | Use bounded channels with DropOldest, optimize mixing loop, target < 100ms total latency |
| Memory pressure from audio buffers | Pool audio buffers using `ArrayPool<byte>`, limit concurrent rooms |
| Opus library compatibility on ARM | Use Concentus (pure C#), test on Raspberry Pi early |
| WebSocket connection stability | Implement reconnection logic in client, heartbeat/ping handling |
| Race conditions in room state | Use `ConcurrentDictionary`, careful locking in connection manager |
| Large broadcast rooms (100+ users) | Implement batched broadcasts, consider chunked state updates |

---

## 10. Testing Strategy

### Unit Tests
- Room service CRUD operations
- Connection manager join/leave
- Audio mixer PCM mixing math
- Message serialization/deserialization
- Options validation

### Integration Tests
- WebSocket handler with mock connections
- REST API endpoints with test server
- Full room lifecycle (create, join, leave, delete)

### Manual Testing
- Multi-client voice communication
- Mobile browser PTT functionality
- Meshtastic bridge (if implemented)
- Network disconnection/reconnection

---

## 11. File Summary

**Total new files: ~35**
- Models: 5
- Options: 2
- Interfaces: 4
- Services: 5
- Connection: 2
- WebSocket: 1
- Endpoints: 1
- Protocol: 8
- Meshtastic Bridge: 2
- DI Extensions: 1
- Contracts/DTOs: 5
- Tests: 5
- Web UI: 1 new + 3 updates

**Files to modify: 5**
- Program.cs
- appsettings.json
- index.html
- style.css
- app.js
