# Voice Chat Rooms Feature

## Overview

Voice chat rooms extend Familiar beyond 1:1 handler-cosplayer communication to support group voice channels. Multiple cosplayers and handlers can join a shared audio room, similar to Discord voice channels or a walkie-talkie group channel.

## Use Cases

1. **Group Cosplay** - Coordinate a group of cosplayers with multiple handlers
2. **Convention Staff** - Event staff communicate across a venue
3. **Performance Coordination** - Director talks to entire cast
4. **Photo Shoots** - Photographer, assistants, and models all connected
5. **Backup Communication** - If WiFi fails, Meshtastic voice relay

## Architecture

### Room Types

| Type | Description | Max Users |
|------|-------------|-----------|
| **Direct** | 1:1 handler ↔ cosplayer (current) | 2 |
| **Team** | One handler, multiple cosplayers | 1 + 10 |
| **Group** | Multiple handlers, multiple cosplayers | 20 |
| **Broadcast** | One speaker, many listeners | 1 + 100 |

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                      Voice Chat Server                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Room A    │  │   Room B    │  │   Room C    │         │
│  │  "Alpha"    │  │  "Bravo"    │  │  "Photo"    │         │
│  │             │  │             │  │             │         │
│  │ 👤 Handler1 │  │ 👤 Handler2 │  │ 👤 Photog   │         │
│  │ 🎭 Cosplay1 │  │ 🎭 Cosplay3 │  │ 👤 Assist   │         │
│  │ 🎭 Cosplay2 │  │ 🎭 Cosplay4 │  │ 🎭 Model1   │         │
│  │             │  │             │  │ 🎭 Model2   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

### Audio Routing

```
                    ┌──────────────┐
   Cosplayer 1 ────►│              │
                    │    Audio     │────► All Room Members
   Cosplayer 2 ────►│    Mixer     │      (except sender)
                    │              │
   Handler 1   ────►│              │
                    └──────────────┘
```

## Configuration

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
      "EnableMeshtasticBridge": true,
      "Rooms": [
        {
          "Id": "main",
          "Name": "Main Channel",
          "Password": null,
          "MaxUsers": 20,
          "Persistent": true
        },
        {
          "Id": "vip",
          "Name": "VIP Room",
          "Password": "secret123",
          "MaxUsers": 5,
          "Persistent": true
        }
      ]
    }
  }
}
```

## API Endpoints

### Room Management

```
GET    /api/voice/rooms                 # List all rooms
POST   /api/voice/rooms                 # Create a room
GET    /api/voice/rooms/{roomId}        # Get room details
DELETE /api/voice/rooms/{roomId}        # Delete a room
```

### Room Membership

```
POST   /api/voice/rooms/{roomId}/join   # Join a room
POST   /api/voice/rooms/{roomId}/leave  # Leave a room
GET    /api/voice/rooms/{roomId}/users  # List users in room
POST   /api/voice/rooms/{roomId}/kick/{userId}  # Kick user (admin)
```

### User Settings

```
POST   /api/voice/mute                  # Mute self
POST   /api/voice/unmute                # Unmute self
POST   /api/voice/deafen                # Deafen (mute incoming)
POST   /api/voice/ptt                   # Push-to-talk state
```

## WebSocket Protocol

### Connection

```
WS /ws/voice?room={roomId}&token={authToken}
```

### Message Types

**Join Room:**
```json
{
  "type": "join",
  "roomId": "main",
  "displayName": "Handler1",
  "role": "handler"
}
```

**User Joined (broadcast):**
```json
{
  "type": "user_joined",
  "userId": "abc123",
  "displayName": "Cosplayer2",
  "role": "cosplayer"
}
```

**Audio Data:**
```json
{
  "type": "audio",
  "userId": "abc123",
  "data": "<base64 opus frames>",
  "sequence": 12345
}
```

Or binary WebSocket frames with header:
```
[1 byte: type=0x01] [4 bytes: userId] [2 bytes: sequence] [N bytes: opus data]
```

**PTT State:**
```json
{
  "type": "ptt",
  "active": true
}
```

**Speaking Indicator:**
```json
{
  "type": "speaking",
  "userId": "abc123",
  "speaking": true
}
```

**Room State:**
```json
{
  "type": "room_state",
  "roomId": "main",
  "users": [
    {"userId": "abc123", "displayName": "Handler1", "role": "handler", "muted": false, "speaking": false},
    {"userId": "def456", "displayName": "Cosplay1", "role": "cosplayer", "muted": false, "speaking": true}
  ]
}
```

## Implementation

### Interfaces

```csharp
public interface IVoiceRoomService
{
    IReadOnlyList<VoiceRoom> Rooms { get; }

    Task<VoiceRoom> CreateRoomAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<bool> DeleteRoomAsync(string roomId, CancellationToken ct = default);
    Task<VoiceRoom?> GetRoomAsync(string roomId, CancellationToken ct = default);
    Task<IReadOnlyList<VoiceRoom>> GetRoomsAsync(CancellationToken ct = default);
}

public interface IVoiceRoomConnection
{
    string UserId { get; }
    string RoomId { get; }
    string DisplayName { get; }
    UserRole Role { get; }
    bool IsMuted { get; }
    bool IsDeafened { get; }
    bool IsSpeaking { get; }

    Task SendAudioAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);
    Task MuteAsync(CancellationToken ct = default);
    Task UnmuteAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}

public record VoiceRoom(
    string Id,
    string Name,
    bool IsPasswordProtected,
    int UserCount,
    int MaxUsers,
    IReadOnlyList<VoiceRoomUser> Users
);

public record VoiceRoomUser(
    string UserId,
    string DisplayName,
    UserRole Role,
    bool IsMuted,
    bool IsSpeaking
);

public enum UserRole { Handler, Cosplayer, Observer }
```

### Audio Mixing

For multiple simultaneous speakers, mix audio server-side:

```csharp
public class AudioMixer
{
    public byte[] MixFrames(IEnumerable<byte[]> opusFrames)
    {
        // Decode each Opus frame to PCM
        var pcmBuffers = opusFrames.Select(DecodeOpus).ToList();

        // Mix PCM samples (sum with clipping prevention)
        var mixed = MixPcm(pcmBuffers);

        // Re-encode to Opus
        return EncodeOpus(mixed);
    }

    private short[] MixPcm(List<short[]> buffers)
    {
        var maxLength = buffers.Max(b => b.Length);
        var result = new int[maxLength];

        foreach (var buffer in buffers)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                result[i] += buffer[i];
            }
        }

        // Normalize to prevent clipping
        return result.Select(s => (short)Math.Clamp(s, short.MinValue, short.MaxValue)).ToArray();
    }
}
```

## Web UI

### Room List

```html
<div class="voice-rooms">
  <h3>Voice Channels</h3>
  <ul id="room-list">
    <li class="room" data-room="main">
      <span class="room-name">🔊 Main Channel</span>
      <span class="room-users">3/20</span>
      <ul class="room-members">
        <li class="speaking">👤 Handler1</li>
        <li>🎭 Cosplayer1</li>
        <li>🎭 Cosplayer2</li>
      </ul>
    </li>
    <li class="room" data-room="vip">
      <span class="room-name">🔒 VIP Room</span>
      <span class="room-users">1/5</span>
    </li>
  </ul>
  <button id="create-room">+ Create Room</button>
</div>
```

### Voice Controls

```html
<div class="voice-controls">
  <button id="ptt-btn" class="ptt">🎤 Push to Talk</button>
  <button id="mute-btn">Mute</button>
  <button id="deafen-btn">Deafen</button>
  <button id="leave-btn">Leave Channel</button>
</div>
```

## Meshtastic Bridge

Optionally bridge voice rooms to Meshtastic for extended range:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Handler    │────►│   Familiar  │────►│  Cosplayer  │
│  (Phone)    │WiFi │    (Pi)     │Mesh │  (Remote)   │
└─────────────┘     └─────────────┘     └─────────────┘
                           │
                    Voice encoded as
                    low-bitrate audio
                    over Meshtastic
```

This allows a cosplayer who is out of WiFi range to still receive voice via Meshtastic (lower quality, higher latency).

## Bandwidth Considerations

| Users | Bitrate | Total Bandwidth |
|-------|---------|-----------------|
| 2 | 32 kbps | 64 kbps |
| 5 | 32 kbps | 160 kbps |
| 10 | 32 kbps | 320 kbps |
| 20 | 32 kbps | 640 kbps |

With server-side mixing, each client only receives one mixed stream regardless of speaker count.

## Security

- Room passwords for private channels
- Kick/ban functionality for room admins
- Rate limiting on audio transmission
- Encryption in transit (WSS)
- User authentication required

## Future Enhancements

- [ ] Spatial audio (positional based on GPS)
- [ ] Voice activation per-room settings
- [ ] Recording of room sessions
- [ ] Text chat alongside voice
- [ ] Screen sharing (Pi 5 camera to room)
- [ ] Bot users for announcements
- [ ] Integration with Discord/other platforms
