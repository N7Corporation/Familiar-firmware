# Familiar Firmware Technical Documentation

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Handler's Phone                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Web Browser / PWA / App                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │   │
│  │  │  Microphone │  │  WebSocket  │  │  Meshtastic    │  │   │
│  │  │   Capture   │──│   Binary    │  │  App (Backup)  │  │   │
│  │  └─────────────┘  └──────┬──────┘  └───────┬─────────┘  │   │
│  └──────────────────────────┼─────────────────┼────────────┘   │
└─────────────────────────────┼─────────────────┼────────────────┘
                              │ WiFi            │ LoRa
                              │                 │
┌─────────────────────────────┼─────────────────┼────────────────┐
│                     Raspberry Pi              │                 │
│  ┌──────────────────────────▼──────┐  ┌───────▼─────────────┐  │
│  │      ASP.NET Core Web Server    │  │   Meshtastic        │  │
│  │  ┌──────────────────────────┐   │  │   ┌─────────────┐   │  │
│  │  │  WebSocket Audio Handler │   │  │   │ Msg Listener│   │  │
│  │  └────────────┬─────────────┘   │  │   └──────┬──────┘   │  │
│  └───────────────┼─────────────────┘  └──────────┼──────────┘  │
│                  │                               │              │
│  ┌───────────────▼───────────────────────────────▼──────────┐  │
│  │                    Audio Manager                          │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐   │  │
│  │  │ Stream Audio│  │    Mixer    │  │   TTS Engine    │   │  │
│  │  └──────┬──────┘  └──────┬──────┘  └────────┬────────┘   │  │
│  └─────────┼────────────────┼──────────────────┼────────────┘  │
│            └────────────────┴──────────────────┘               │
│                             │                                   │
│                    ┌────────▼────────┐                         │
│                    │  Audio Output   │                         │
│                    │ (Speaker/DAC)   │                         │
│                    └─────────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

## Hardware Components

### Raspberry Pi Selection

| Model | Pros | Cons | Recommendation |
|-------|------|------|----------------|
| Pi Zero 2 W | Compact, low power, built-in WiFi | Limited processing, single USB | Best for size-constrained builds |
| Pi 3B+ | Good balance, proven reliability | Larger form factor | Good general choice |
| Pi 4 | Most powerful, multiple USB | Higher power consumption, heat | Best for development |
| Pi 5 | Latest, most powerful | Highest power draw | Overkill for this use case |

**Note:** .NET 8 requires 64-bit OS. Pi Zero 2 W and newer support ARM64.

### LoRa Module Options

**Recommended: RAK Wireless RAK4631 or similar SX1262-based module**

| Module | Frequency | Interface | Notes |
|--------|-----------|-----------|-------|
| RAK4631 | 868/915 MHz | USB/UART | Meshtastic-ready |
| LILYGO T-Beam | 868/915 MHz | USB | Has GPS, bulkier |
| Heltec LoRa 32 | 868/915 MHz | USB | Budget option |
| Standalone SX1262 | 868/915 MHz | SPI | Requires more setup |

**Frequency Selection:**
- 915 MHz: Americas (FCC)
- 868 MHz: Europe (CE)
- 923 MHz: Asia

### Audio Output Options

| Method | Quality | Complexity | Notes |
|--------|---------|------------|-------|
| 3.5mm jack | Medium | Low | Built into Pi (not Zero) |
| USB Audio | High | Low | Plug and play |
| I2S DAC | High | Medium | Best quality, needs soldering |
| Bluetooth | Variable | Medium | Wireless, latency concerns |

**Recommended DACs:**
- PCM5102 I2S DAC (high quality, ~$5)
- MAX98357A I2S Amp (DAC + amplifier, ~$8)
- USB sound card (simple, ~$10)

### Power Requirements

| Component | Current Draw | Notes |
|-----------|--------------|-------|
| Pi Zero 2 W | 100-400 mA | Idle to active |
| Pi 4 | 500-1200 mA | Idle to active |
| LoRa Module | 20-120 mA | Receive to transmit |
| Audio Amp | 50-200 mA | Depends on volume |

**Battery Recommendations:**
- 5000 mAh: ~6-10 hours operation
- 10000 mAh: ~12-20 hours operation
- Use quality 5V/3A power bank

---

## Software Architecture

### Solution Structure

```
Familiar-firmware/
├── Familiar.sln
├── src/
│   ├── Familiar.Host/              # Main ASP.NET Core application
│   │   ├── Program.cs              # Entry point and DI configuration
│   │   ├── appsettings.json        # Configuration
│   │   ├── Endpoints/              # Minimal API endpoints
│   │   │   ├── StatusEndpoints.cs
│   │   │   ├── ConfigEndpoints.cs
│   │   │   └── TtsEndpoints.cs
│   │   ├── WebSockets/             # WebSocket handlers
│   │   │   └── AudioWebSocketHandler.cs
│   │   └── wwwroot/                # Static web files
│   │       ├── index.html
│   │       ├── app.js
│   │       └── style.css
│   │
│   ├── Familiar.Audio/             # Audio processing library
│   │   ├── Familiar.Audio.csproj
│   │   ├── IAudioManager.cs
│   │   ├── AudioManager.cs
│   │   ├── IAudioPlayer.cs
│   │   ├── AlsaAudioPlayer.cs      # ALSA P/Invoke wrapper
│   │   └── AudioBuffer.cs
│   │
│   ├── Familiar.Meshtastic/        # Meshtastic integration
│   │   ├── Familiar.Meshtastic.csproj
│   │   ├── IMeshtasticClient.cs
│   │   ├── MeshtasticClient.cs
│   │   ├── MeshtasticService.cs    # IHostedService
│   │   ├── MessageHandler.cs
│   │   └── Protobuf/               # Generated protobuf classes
│   │
│   └── Familiar.Tts/               # Text-to-speech
│       ├── Familiar.Tts.csproj
│       ├── ITtsEngine.cs
│       ├── EspeakTtsEngine.cs
│       └── TtsOptions.cs
│
├── tests/
│   ├── Familiar.Audio.Tests/
│   ├── Familiar.Meshtastic.Tests/
│   └── Familiar.Tts.Tests/
│
├── config/
│   └── appsettings.Production.json
│
└── scripts/
    ├── setup.sh
    ├── install-service.sh
    └── familiar.service            # systemd unit file
```

### Core Interfaces and Classes

#### 1. Audio Manager (`Familiar.Audio`)

```csharp
// IAudioManager.cs
public interface IAudioManager
{
    Task PlayStreamAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);
    Task PlayTtsAsync(string text, int priority = 0, CancellationToken ct = default);
    void SetVolume(float level);
    float Volume { get; }
    bool IsMuted { get; set; }
}

// AudioManager.cs
public class AudioManager : IAudioManager
{
    private readonly IAudioPlayer _player;
    private readonly ITtsEngine _tts;
    private readonly Channel<AudioCommand> _commandChannel;
    private readonly ILogger<AudioManager> _logger;

    public AudioManager(
        IAudioPlayer player,
        ITtsEngine tts,
        ILogger<AudioManager> logger)
    {
        _player = player;
        _tts = tts;
        _logger = logger;
        _commandChannel = Channel.CreateBounded<AudioCommand>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    public async Task PlayStreamAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default)
    {
        await _player.WriteAsync(audioData, ct);
    }

    public async Task PlayTtsAsync(string text, int priority = 0, CancellationToken ct = default)
    {
        var sanitized = SanitizeInput(text);
        var audioData = await _tts.SynthesizeAsync(sanitized, ct);
        await _player.WriteAsync(audioData, ct);
    }

    private static string SanitizeInput(string text)
    {
        // Remove potentially problematic characters
        var sanitized = Regex.Replace(text, @"[<>{}]", "");
        // Limit length
        return sanitized.Length > 500 ? sanitized[..500] : sanitized;
    }
}
```

#### 2. Web Server (`Familiar.Host`)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<FamiliarOptions>(
    builder.Configuration.GetSection("Familiar"));

// Services
builder.Services.AddSingleton<IAudioManager, AudioManager>();
builder.Services.AddSingleton<IAudioPlayer, AlsaAudioPlayer>();
builder.Services.AddSingleton<ITtsEngine, EspeakTtsEngine>();
builder.Services.AddSingleton<IMeshtasticClient, MeshtasticClient>();
builder.Services.AddHostedService<MeshtasticService>();

var app = builder.Build();

// Static files
app.UseDefaultFiles();
app.UseStaticFiles();

// WebSocket
app.UseWebSockets();
app.Map("/ws/audio", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var handler = context.RequestServices.GetRequiredService<AudioWebSocketHandler>();
        await handler.HandleAsync(context);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// API endpoints
app.MapStatusEndpoints();
app.MapConfigEndpoints();
app.MapTtsEndpoints();

app.Run();
```

```csharp
// Endpoints/StatusEndpoints.cs
public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        app.MapGet("/api/status", (IAudioManager audio, IMeshtasticClient mesh) =>
        {
            return Results.Ok(new
            {
                Status = "Online",
                AudioVolume = audio.Volume,
                AudioMuted = audio.IsMuted,
                MeshtasticConnected = mesh.IsConnected,
                MeshtasticNodeCount = mesh.KnownNodes.Count
            });
        });
    }
}
```

#### 3. Meshtastic Client (`Familiar.Meshtastic`)

```csharp
// IMeshtasticClient.cs
public interface IMeshtasticClient
{
    bool IsConnected { get; }
    IReadOnlyList<MeshtasticNode> KnownNodes { get; }
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
}

// MeshtasticClient.cs
public class MeshtasticClient : IMeshtasticClient
{
    private readonly SerialPort _serialPort;
    private readonly ILogger<MeshtasticClient> _logger;
    private readonly MeshtasticOptions _options;

    public MeshtasticClient(
        IOptions<MeshtasticOptions> options,
        ILogger<MeshtasticClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _serialPort = new SerialPort(_options.Port, _options.BaudRate);
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public bool IsConnected => _serialPort.IsOpen;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _serialPort.Open();
        _serialPort.DataReceived += OnDataReceived;
        _logger.LogInformation("Connected to Meshtastic on {Port}", _options.Port);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // Read and parse Meshtastic protobuf messages
        var buffer = new byte[_serialPort.BytesToRead];
        _serialPort.Read(buffer, 0, buffer.Length);

        // Parse message and raise event
        if (TryParseMessage(buffer, out var message))
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }
    }
}

// MeshtasticService.cs
public class MeshtasticService : BackgroundService
{
    private readonly IMeshtasticClient _client;
    private readonly IAudioManager _audio;
    private readonly ILogger<MeshtasticService> _logger;

    public MeshtasticService(
        IMeshtasticClient client,
        IAudioManager audio,
        ILogger<MeshtasticService> logger)
    {
        _client = client;
        _audio = audio;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.MessageReceived += OnMessageReceived;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await _client.ConnectAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Meshtastic connection error");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        _logger.LogInformation("Message from {Node}: {Text}", e.FromNode, e.Text);

        if (e.Text.StartsWith("!"))
        {
            await HandleCommand(e.Text, e.FromNode);
        }
        else
        {
            await _audio.PlayTtsAsync(e.Text);
        }
    }
}
```

#### 4. TTS Engine (`Familiar.Tts`)

```csharp
// ITtsEngine.cs
public interface ITtsEngine
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default);
    void SetVoice(string voiceId);
    void SetRate(int wordsPerMinute);
}

// EspeakTtsEngine.cs
public class EspeakTtsEngine : ITtsEngine
{
    private readonly TtsOptions _options;
    private readonly ILogger<EspeakTtsEngine> _logger;

    public EspeakTtsEngine(
        IOptions<TtsOptions> options,
        ILogger<EspeakTtsEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName() + ".wav";

        try
        {
            var args = $"-v {_options.Voice} -s {_options.Rate} -w \"{tempFile}\" \"{text}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "espeak",
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("espeak failed: {Error}", error);
                return Array.Empty<byte>();
            }

            return await File.ReadAllBytesAsync(tempFile, ct);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
```

#### 5. WebSocket Audio Handler

```csharp
// WebSockets/AudioWebSocketHandler.cs
public class AudioWebSocketHandler
{
    private readonly IAudioManager _audio;
    private readonly ILogger<AudioWebSocketHandler> _logger;

    public AudioWebSocketHandler(
        IAudioManager audio,
        ILogger<AudioWebSocketHandler> logger)
    {
        _audio = audio;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[8192];

        _logger.LogInformation("Audio WebSocket connected");

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var audioData = buffer.AsMemory(0, result.Count);
                    await _audio.PlayStreamAsync(audioData, context.RequestAborted);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Handle control messages (start, stop, etc.)
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleControlMessage(ws, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error");
        }

        _logger.LogInformation("Audio WebSocket disconnected");
    }

    private async Task HandleControlMessage(WebSocket ws, string message)
    {
        var json = JsonDocument.Parse(message);
        var type = json.RootElement.GetProperty("type").GetString();

        switch (type)
        {
            case "start":
                await SendJsonAsync(ws, new { type = "ready" });
                break;
            case "stop":
                await SendJsonAsync(ws, new { type = "stopped" });
                break;
        }
    }

    private static async Task SendJsonAsync(WebSocket ws, object data)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data);
        await ws.SendAsync(json, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
```

---

## Communication Protocols

### WiFi Audio Streaming (WebSocket)

**Protocol:**
```
Client → Server: Binary audio frames (PCM/Opus)
Server → Client: JSON status messages

Audio Format:
- Codec: PCM16 (simple) or Opus (compressed)
- Sample Rate: 48000 Hz
- Channels: Mono
- Frame Size: 20ms (960 samples at 48kHz)
```

**Message Flow:**
```
1. Client connects to wss://device/ws/audio
2. Client sends: {"type": "start", "codec": "pcm16", "sampleRate": 48000}
3. Server responds: {"type": "ready"}
4. Client sends: Binary audio frames
5. Client sends: {"type": "stop"} when done
6. Server responds: {"type": "stopped"}
```

### Meshtastic Protocol

**Message Format:**
Standard Meshtastic text message to device node.

**Special Commands:**
```
!vol 80       - Set volume to 80%
!voice 2      - Switch to voice preset 2
!status       - Request status (replies via Meshtastic)
!mute         - Mute audio output
!unmute       - Unmute audio output
```

---

## Web Interface

### Handler Interface Design

```
┌────────────────────────────────────────┐
│  Familiar                    ● Online  │
├────────────────────────────────────────┤
│                                        │
│         ┌──────────────────┐           │
│         │                  │           │
│         │   HOLD TO TALK   │           │
│         │                  │           │
│         └──────────────────┘           │
│                                        │
│  ━━━━━━━━━━━━━━━━━━━●━━━━━  Volume    │
│                                        │
├────────────────────────────────────────┤
│  Settings                              │
└────────────────────────────────────────┘
```

### Frontend Implementation

```javascript
// wwwroot/app.js
class FamiliarClient {
    constructor() {
        this.ws = null;
        this.audioContext = null;
        this.mediaStream = null;
        this.processor = null;
    }

    async connect() {
        const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.ws = new WebSocket(`${protocol}//${location.host}/ws/audio`);
        this.ws.binaryType = 'arraybuffer';

        this.ws.onopen = () => this.onConnected();
        this.ws.onclose = () => this.onDisconnected();
        this.ws.onerror = (e) => console.error('WebSocket error:', e);
    }

    async startTalking() {
        this.mediaStream = await navigator.mediaDevices.getUserMedia({
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                sampleRate: 48000
            }
        });

        this.audioContext = new AudioContext({ sampleRate: 48000 });
        const source = this.audioContext.createMediaStreamSource(this.mediaStream);

        await this.audioContext.audioWorklet.addModule('audio-processor.js');
        this.processor = new AudioWorkletNode(this.audioContext, 'audio-processor');

        this.processor.port.onmessage = (e) => {
            if (this.ws?.readyState === WebSocket.OPEN) {
                this.ws.send(e.data);
            }
        };

        source.connect(this.processor);
        this.sendControl({ type: 'start', codec: 'pcm16', sampleRate: 48000 });
    }

    stopTalking() {
        this.sendControl({ type: 'stop' });
        this.processor?.disconnect();
        this.mediaStream?.getTracks().forEach(t => t.stop());
        this.processor = null;
        this.mediaStream = null;
    }

    sendControl(data) {
        if (this.ws?.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(data));
        }
    }

    onConnected() {
        document.getElementById('status').textContent = 'Online';
        document.getElementById('status').classList.add('online');
    }

    onDisconnected() {
        document.getElementById('status').textContent = 'Offline';
        document.getElementById('status').classList.remove('online');
    }
}

// Initialize
const client = new FamiliarClient();
client.connect();

// Push-to-talk button
const talkBtn = document.getElementById('talk-btn');
talkBtn.addEventListener('mousedown', () => client.startTalking());
talkBtn.addEventListener('mouseup', () => client.stopTalking());
talkBtn.addEventListener('touchstart', (e) => { e.preventDefault(); client.startTalking(); });
talkBtn.addEventListener('touchend', () => client.stopTalking());
```

---

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      },
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/etc/familiar/cert.pem",
          "KeyPath": "/etc/familiar/key.pem"
        }
      }
    }
  },
  "Familiar": {
    "Audio": {
      "OutputDevice": "default",
      "SampleRate": 48000,
      "BufferSize": 1024,
      "Volume": 0.8
    },
    "Meshtastic": {
      "Enabled": true,
      "Port": "/dev/ttyUSB0",
      "BaudRate": 115200,
      "NodeName": "Familiar",
      "Channel": 0,
      "AllowedNodes": []
    },
    "Tts": {
      "Engine": "espeak",
      "Voice": "en",
      "Rate": 150
    }
  }
}
```

### Configuration Classes

```csharp
// FamiliarOptions.cs
public class FamiliarOptions
{
    public AudioOptions Audio { get; set; } = new();
    public MeshtasticOptions Meshtastic { get; set; } = new();
    public TtsOptions Tts { get; set; } = new();
}

public class AudioOptions
{
    public string OutputDevice { get; set; } = "default";
    public int SampleRate { get; set; } = 48000;
    public int BufferSize { get; set; } = 1024;
    public float Volume { get; set; } = 0.8f;
}

public class MeshtasticOptions
{
    public bool Enabled { get; set; } = true;
    public string Port { get; set; } = "/dev/ttyUSB0";
    public int BaudRate { get; set; } = 115200;
    public string NodeName { get; set; } = "Familiar";
    public int Channel { get; set; } = 0;
    public List<string> AllowedNodes { get; set; } = new();
}

public class TtsOptions
{
    public string Engine { get; set; } = "espeak";
    public string Voice { get; set; } = "en";
    public int Rate { get; set; } = 150;
}
```

---

## Deployment

### systemd Service

```ini
# /etc/systemd/system/familiar.service
[Unit]
Description=Familiar Firmware
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/familiar
ExecStart=/opt/familiar/Familiar.Host
Restart=always
RestartSec=10
User=familiar
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443

[Install]
WantedBy=multi-user.target
```

### Setup Script

```bash
#!/bin/bash
# scripts/setup.sh

set -e

echo "Installing .NET 8 Runtime..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime aspnetcore

echo "Installing dependencies..."
sudo apt-get update
sudo apt-get install -y espeak alsa-utils

echo "Building application..."
dotnet publish src/Familiar.Host -c Release -o /opt/familiar

echo "Creating service user..."
sudo useradd -r -s /bin/false familiar || true

echo "Setting permissions..."
sudo chown -R familiar:familiar /opt/familiar
sudo usermod -a -G audio,dialout familiar

echo "Installing systemd service..."
sudo cp scripts/familiar.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable familiar

echo "Setup complete! Start with: sudo systemctl start familiar"
```

---

## Security Considerations

### Authentication

```csharp
// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
```

### Input Sanitization

```csharp
public static class InputSanitizer
{
    public static string SanitizeTtsInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove shell-dangerous characters
        text = Regex.Replace(text, @"[;|&`$(){}[\]<>]", "");

        // Remove SSML-like tags
        text = Regex.Replace(text, @"<[^>]+>", "");

        // Limit length
        return text.Length > 500 ? text[..500] : text;
    }
}
```

---

## Performance Optimization

### Latency Targets

| Path | Target | Acceptable |
|------|--------|------------|
| WiFi Audio | < 100ms | < 200ms |
| TTS Processing | < 500ms | < 1000ms |
| End-to-end (WiFi) | < 150ms | < 300ms |
| End-to-end (LoRa) | < 2s | < 5s |

### Optimization Strategies

1. **Use ArrayPool<T>**: Reduce allocations in audio path
2. **Channels over Queues**: Better for producer-consumer patterns
3. **Span<T>/Memory<T>**: Avoid copying audio data
4. **Native AOT**: Consider for faster startup (limited compatibility)

```csharp
// Efficient buffer handling
public class AudioBuffer
{
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    public void ProcessAudio(ReadOnlySpan<byte> input)
    {
        var buffer = _pool.Rent(input.Length);
        try
        {
            input.CopyTo(buffer);
            // Process...
        }
        finally
        {
            _pool.Return(buffer);
        }
    }
}
```

---

## Testing

### Unit Tests

```csharp
// tests/Familiar.Tts.Tests/EspeakTtsEngineTests.cs
public class EspeakTtsEngineTests
{
    [Fact]
    public async Task SynthesizeAsync_WithValidText_ReturnsAudioData()
    {
        var options = Options.Create(new TtsOptions { Voice = "en", Rate = 150 });
        var logger = NullLogger<EspeakTtsEngine>.Instance;
        var engine = new EspeakTtsEngine(options, logger);

        var result = await engine.SynthesizeAsync("Hello world");

        Assert.NotEmpty(result);
    }
}

// tests/Familiar.Audio.Tests/InputSanitizerTests.cs
public class InputSanitizerTests
{
    [Theory]
    [InlineData("<script>alert('xss')</script>", "alert('xss')")]
    [InlineData("Hello; rm -rf /", "Hello rm -rf /")]
    [InlineData("Normal message", "Normal message")]
    public void SanitizeTtsInput_RemovesDangerousContent(string input, string expected)
    {
        var result = InputSanitizer.SanitizeTtsInput(input);
        Assert.Equal(expected, result);
    }
}
```

### Integration Tests

```csharp
// tests/Familiar.Host.Tests/WebSocketTests.cs
public class WebSocketTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebSocketTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebSocket_Connect_ReturnsReady()
    {
        var client = _factory.CreateClient();
        var wsClient = _factory.Server.CreateWebSocketClient();

        var ws = await wsClient.ConnectAsync(
            new Uri("ws://localhost/ws/audio"),
            CancellationToken.None);

        await ws.SendAsync(
            Encoding.UTF8.GetBytes("{\"type\":\"start\"}"),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        var buffer = new byte[1024];
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

        Assert.Contains("ready", response);
    }
}
```

---

## API Reference

### REST Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/status` | GET | Device status and info |
| `/api/config` | GET | Current configuration |
| `/api/config` | PUT | Update configuration |
| `/api/volume` | POST | Set volume level |
| `/api/tts` | POST | Speak text via TTS |
| `/api/meshtastic/nodes` | GET | List known nodes |

### WebSocket Endpoints

| Endpoint | Description |
|----------|-------------|
| `/ws/audio` | Audio streaming |

### Example API Calls

```bash
# Get status
curl https://familiar.local/api/status

# Set volume
curl -X POST https://familiar.local/api/volume \
  -H "Content-Type: application/json" \
  -d '{"level": 0.75}'

# Send TTS message
curl -X POST https://familiar.local/api/tts \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello from the API"}'
```

---

## Troubleshooting

### Common Issues

| Issue | Possible Cause | Solution |
|-------|---------------|----------|
| No audio output | Wrong device selected | Check `aplay -l`, update config |
| High latency | Buffer too large | Reduce BufferSize in config |
| Meshtastic not connecting | Wrong port | Check `ls /dev/tty*`, update config |
| TTS not working | espeak not installed | `sudo apt install espeak` |
| Web interface not loading | HTTPS certificate issue | Regenerate certificates |
| .NET not starting | Wrong architecture | Ensure 64-bit OS on Pi |

### Debug Commands

```bash
# Check .NET installation
dotnet --info

# Check audio devices
aplay -l
arecord -l

# Check USB devices
lsusb

# Check Meshtastic
ls /dev/tty*

# Check service status
sudo systemctl status familiar

# View logs
sudo journalctl -u familiar -f

# Test TTS
espeak "Testing one two three"
```
