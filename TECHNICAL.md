# Familiar Firmware Technical Documentation

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Handler's Phone                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Web Browser / PWA / App                     │   │
│  │                                                          │   │
│  │  ┌─────────────┐              ┌─────────────────┐       │   │
│  │  │  Microphone │──┐      ┌───►│    Speaker      │       │   │
│  │  └─────────────┘  │      │    └─────────────────┘       │   │
│  │                   ▼      │                               │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │         WebSocket (Two-Way Audio)               │    │   │
│  │  │    /ws/audio/down ──►     ◄── /ws/audio/up      │    │   │
│  │  └──────────────────────┬──────────────────────────┘    │   │
│  │                         │         ┌─────────────────┐   │   │
│  │                         │         │  Meshtastic App │   │   │
│  │                         │         │    (Backup)     │   │   │
│  │                         │         └───────┬─────────┘   │   │
│  └─────────────────────────┼─────────────────┼─────────────┘   │
└─────────────────────────────┼─────────────────┼────────────────┘
                              │                 │
            Connects to Pi's  │ WiFi AP         │ LoRa
            WiFi: "Familiar"  │ 192.168.4.1     │
                              │                 │
┌─────────────────────────────┼─────────────────┼────────────────┐
│                     Raspberry Pi              │                 │
│  ┌──────────────────────────┐                 │                 │
│  │  WiFi Access Point       │                 │                 │
│  │  (hostapd + dnsmasq)     │                 │                 │
│  └────────────┬─────────────┘                 │                 │
│               │                               │                 │
│  ┌────────────▼─────────────┐  ┌──────────────▼──────────────┐ │
│  │  ASP.NET Core Web Server │  │   Meshtastic                │ │
│  │  ┌──────────────────┐    │  │   ┌─────────────┐           │ │
│  │  │ WS Audio Down ▼  │    │  │   │ Msg Listener│           │ │
│  │  │ WS Audio Up   ▲  │    │  │   └──────┬──────┘           │ │
│  │  └────────┬─────────┘    │  │          │                  │ │
│  └───────────┼──────────────┘  └──────────┼──────────────────┘ │
│              │                            │                     │
│  ┌───────────▼────────────────────────────▼──────────────────┐ │
│  │                    Audio Manager                           │ │
│  │  ┌──────────┐ ┌─────────┐ ┌───────┐ ┌─────────────────┐   │ │
│  │  │ Playback │ │ Capture │ │ Mixer │ │   TTS Engine    │   │ │
│  │  │ (Down)   │ │  (Up)   │ │       │ │                 │   │ │
│  │  └────┬─────┘ └────▲────┘ └───┬───┘ └────────┬────────┘   │ │
│  └───────┼────────────┼──────────┼──────────────┼────────────┘ │
│          │            │          │              │               │
│          ▼            │          ▼              │               │
│  ┌───────────────┐    │    ┌─────────────────┐  │               │
│  │ Audio Output  │    │    │ Audio Input     │  │               │
│  │ (Speaker/DAC) │    └────│ (Microphone)    │◄─┘               │
│  └───────────────┘         └─────────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
```

## Hardware Components

### Raspberry Pi Selection

| Model | RAM | Use Case | Notes |
|-------|-----|----------|-------|
| **Pi 4** | 2GB+ | DIY / Minimum Spec | Recommended minimum for reliable performance |
| **Pi 5** | 4GB+ | Commercial Version | Better CPU, lower latency, improved I/O |

**Why Pi 4 Minimum:**
- .NET 8 requires 64-bit ARM (ARMv8)
- Sufficient CPU for audio processing + web server + Meshtastic
- Built-in WiFi supports AP mode reliably
- Multiple USB ports for LoRa module + audio

**Why Pi 5 for Commercial:**
- 2-3x faster CPU = lower audio latency
- Improved WiFi performance
- Better thermal management
- PCIe support for future expansion
- Longer support lifecycle

**Not Recommended:**
- Pi Zero 2 W: Insufficient for concurrent audio streaming + TTS
- Pi 3B+: Limited RAM, older CPU architecture

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

### Microphone Input Options

| Method | Quality | Complexity | Notes |
|--------|---------|------------|-------|
| USB Sound Card | Good | Low | Combined mic in + audio out, plug and play |
| USB Microphone | Good | Low | Dedicated mic, easy setup |
| I2S MEMS Mic | Excellent | Medium | INMP441, SPH0645, best for integration |
| 3.5mm + USB Adapter | Variable | Low | Use existing lavalier mic |

**Recommended for DIY:**
- USB sound card with mic input (~$10) - simplest, handles both input and output

**Recommended for Commercial (Pi 5):**
- I2S MEMS microphone (INMP441 or SPH0645) integrated on custom PCB
- Paired with I2S DAC for full-duplex audio on single I2S bus

**Microphone Placement Tips:**
- Position near mouth but hidden (collar, mask edge, wig)
- Use foam windscreen to reduce breath noise
- Keep away from speaker to minimize feedback

### Power Requirements

| Component | Current Draw | Notes |
|-----------|--------------|-------|
| Pi 4 | 600-1200 mA | Idle to active streaming |
| Pi 5 | 800-1500 mA | Idle to active streaming |
| LoRa Module | 20-120 mA | Receive to transmit |
| Audio Amp | 50-200 mA | Depends on volume |

**Battery Recommendations:**

| Battery | Pi 4 Runtime | Pi 5 Runtime |
|---------|--------------|--------------|
| 5000 mAh | ~4-6 hours | ~3-5 hours |
| 10000 mAh | ~8-12 hours | ~6-10 hours |
| 20000 mAh | ~16-24 hours | ~12-18 hours |

- Use quality 5V 3A power bank (PD/QC compatible for Pi 5)
- Pi 5 requires 5V 5A for full performance (27W USB-C PD)
- Consider battery with passthrough charging for all-day events

### Camera Options (Pi 5 Commercial Only)

| Camera | Resolution | FOV | Notes |
|--------|------------|-----|-------|
| Pi Camera Module 3 | 12MP / 1080p60 | 66° | Standard, autofocus |
| Pi Camera Module 3 Wide | 12MP / 1080p60 | 102° | Better for POV |
| Pi Camera Module 3 NoIR | 12MP / 1080p60 | 66° | Low-light with IR filter removed |

**Recommended: Camera Module 3 Wide** for POV recording (wider field of view captures more of what the cosplayer sees).

**Why Pi 5 Only:**
- Hardware H.264/HEVC encoding via VideoCore VII
- Sufficient CPU for simultaneous audio + video streaming
- New camera connector with higher bandwidth
- Pi 4 lacks resources for reliable video + audio + web server

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
│   │   ├── AlsaAudioPlayer.cs      # ALSA playback P/Invoke
│   │   ├── IAudioCapture.cs
│   │   ├── AlsaAudioCapture.cs     # ALSA capture P/Invoke
│   │   ├── VoiceActivityDetector.cs
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
│   ├── Familiar.Tts/               # Text-to-speech
│   │   ├── Familiar.Tts.csproj
│   │   ├── ITtsEngine.cs
│   │   ├── EspeakTtsEngine.cs
│   │   └── TtsOptions.cs
│   │
│   └── Familiar.Camera/            # Camera streaming (Pi 5 only)
│       ├── Familiar.Camera.csproj
│       ├── ICameraService.cs
│       ├── CameraService.cs
│       ├── CameraOptions.cs
│       └── Recording/
│           ├── RecordingManager.cs
│           └── VideoEncoder.cs
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
    // Playback (Handler → Cosplayer)
    Task PlayStreamAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);
    Task PlayTtsAsync(string text, int priority = 0, CancellationToken ct = default);
    void SetVolume(float level);
    float Volume { get; }
    bool IsMuted { get; set; }

    // Capture (Cosplayer → Handler)
    bool IsCapturing { get; }
    Task StartCaptureAsync(CancellationToken ct = default);
    Task StopCaptureAsync();
    IAsyncEnumerable<byte[]> GetCapturedAudioAsync(CancellationToken ct = default);
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

// IAudioCapture.cs
public interface IAudioCapture
{
    bool IsCapturing { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    IAsyncEnumerable<byte[]> ReadFramesAsync(CancellationToken ct = default);
}

// AlsaAudioCapture.cs
public class AlsaAudioCapture : IAudioCapture, IDisposable
{
    private readonly AudioOptions _options;
    private readonly ILogger<AlsaAudioCapture> _logger;
    private readonly Channel<byte[]> _frameChannel;
    private Process? _arecordProcess;

    public AlsaAudioCapture(
        IOptions<AudioOptions> options,
        ILogger<AlsaAudioCapture> logger)
    {
        _options = options.Value;
        _logger = logger;
        _frameChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    public bool IsCapturing => _arecordProcess?.HasExited == false;

    public async Task StartAsync(CancellationToken ct = default)
    {
        // Use arecord for ALSA capture
        var args = $"-D {_options.InputDevice} -f S16_LE -r {_options.SampleRate} -c 1 -t raw -";

        _arecordProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "arecord",
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _arecordProcess.Start();
        _ = ReadCaptureStreamAsync(_arecordProcess.StandardOutput.BaseStream, ct);

        _logger.LogInformation("Audio capture started on {Device}", _options.InputDevice);
    }

    private async Task ReadCaptureStreamAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[_options.BufferSize];

        while (!ct.IsCancellationRequested && _arecordProcess?.HasExited == false)
        {
            var bytesRead = await stream.ReadAsync(buffer, ct);
            if (bytesRead > 0)
            {
                var frame = buffer[..bytesRead].ToArray();
                await _frameChannel.Writer.WriteAsync(frame, ct);
            }
        }
    }

    public async IAsyncEnumerable<byte[]> ReadFramesAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var frame in _frameChannel.Reader.ReadAllAsync(ct))
        {
            yield return frame;
        }
    }
}

// VoiceActivityDetector.cs
public class VoiceActivityDetector
{
    private readonly float _threshold;
    private readonly int _holdFrames;
    private int _silentFrames;

    public VoiceActivityDetector(float threshold = 0.02f, int holdFrames = 10)
    {
        _threshold = threshold;
        _holdFrames = holdFrames;
    }

    public bool IsVoiceActive(ReadOnlySpan<byte> audioFrame)
    {
        // Calculate RMS energy of the frame
        float sum = 0;
        for (int i = 0; i < audioFrame.Length - 1; i += 2)
        {
            short sample = (short)(audioFrame[i] | (audioFrame[i + 1] << 8));
            float normalized = sample / 32768f;
            sum += normalized * normalized;
        }

        float rms = MathF.Sqrt(sum / (audioFrame.Length / 2));

        if (rms > _threshold)
        {
            _silentFrames = 0;
            return true;
        }

        _silentFrames++;
        return _silentFrames < _holdFrames; // Hold for a bit after voice stops
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

#### 5. Camera Service (`Familiar.Camera` - Pi 5 Only)

```csharp
// ICameraService.cs
public interface ICameraService
{
    bool IsAvailable { get; }
    bool IsStreaming { get; }
    bool IsRecording { get; }

    Task<bool> StartStreamingAsync(CancellationToken ct = default);
    Task StopStreamingAsync();
    Task<bool> StartRecordingAsync(string filename, CancellationToken ct = default);
    Task<string?> StopRecordingAsync();
    Task<byte[]?> CaptureSnapshotAsync(CancellationToken ct = default);

    IAsyncEnumerable<byte[]> GetStreamFramesAsync(CancellationToken ct = default);
}

// CameraService.cs
public class CameraService : ICameraService, IDisposable
{
    private readonly CameraOptions _options;
    private readonly ILogger<CameraService> _logger;
    private Process? _libcameraProcess;
    private Process? _recordingProcess;
    private readonly Channel<byte[]> _frameChannel;

    public CameraService(
        IOptions<CameraOptions> options,
        ILogger<CameraService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _frameChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(30) // ~1 second buffer at 30fps
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    public bool IsAvailable => File.Exists("/usr/bin/libcamera-vid");

    public async Task<bool> StartStreamingAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Camera not available - libcamera not found");
            return false;
        }

        // Use libcamera-vid for hardware-encoded H.264 streaming
        var args = $"-t 0 --inline --width {_options.Width} --height {_options.Height} " +
                   $"--framerate {_options.Framerate} --codec h264 -o -";

        _libcameraProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "libcamera-vid",
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _libcameraProcess.Start();
        _ = ReadFramesAsync(_libcameraProcess.StandardOutput.BaseStream, ct);

        _logger.LogInformation("Camera streaming started at {Width}x{Height}@{Fps}",
            _options.Width, _options.Height, _options.Framerate);

        return true;
    }

    public async Task<bool> StartRecordingAsync(string filename, CancellationToken ct = default)
    {
        var outputPath = Path.Combine(_options.RecordingPath, $"{filename}.mp4");

        var args = $"-t 0 --width {_options.Width} --height {_options.Height} " +
                   $"--framerate {_options.Framerate} --codec h264 -o \"{outputPath}\"";

        _recordingProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "libcamera-vid",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _recordingProcess.Start();
        _logger.LogInformation("Recording started: {Path}", outputPath);

        return true;
    }

    public async Task<byte[]?> CaptureSnapshotAsync(CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName() + ".jpg";

        try
        {
            var args = $"--width {_options.Width} --height {_options.Height} -o \"{tempFile}\"";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "libcamera-jpeg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            await process!.WaitForExitAsync(ct);

            if (process.ExitCode == 0 && File.Exists(tempFile))
            {
                return await File.ReadAllBytesAsync(tempFile, ct);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        return null;
    }
}

// CameraOptions.cs
public class CameraOptions
{
    public bool Enabled { get; set; } = false;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Framerate { get; set; } = 30;
    public string RecordingPath { get; set; } = "/home/familiar/recordings";
    public int StreamBitrate { get; set; } = 4_000_000; // 4 Mbps
    public int RecordingBitrate { get; set; } = 8_000_000; // 8 Mbps
}
```

#### 6. WebSocket Audio Handler

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

### Two-Way Audio Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Handler's Phone                             │
│                                                                  │
│   Microphone ──► /ws/audio/down ──────────────►  Pi Speaker     │
│   Speaker    ◄── /ws/audio/up   ◄──────────────  Pi Microphone  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Downlink: Handler → Cosplayer (`/ws/audio/down`)

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
1. Client connects to ws://192.168.4.1/ws/audio/down
2. Client sends: {"type": "start", "codec": "pcm16", "sampleRate": 48000}
3. Server responds: {"type": "ready"}
4. Client sends: Binary audio frames (while PTT held)
5. Client sends: {"type": "stop"} when done
6. Server responds: {"type": "stopped"}
```

### Uplink: Cosplayer → Handler (`/ws/audio/up`)

**Protocol:**
```
Server → Client: Binary audio frames (PCM16)
Client → Server: JSON control messages

Audio Format:
- Codec: PCM16
- Sample Rate: 48000 Hz
- Channels: Mono
```

**Message Flow:**
```
1. Client connects to ws://192.168.4.1/ws/audio/up
2. Client sends: {"type": "subscribe"}
3. Server responds: {"type": "subscribed", "mode": "vox"}
4. Server sends: Binary audio frames (when voice detected or PTT active)
5. Server sends: {"type": "vox_start"} when voice activity begins
6. Server sends: {"type": "vox_end"} when voice activity ends
```

### Microphone Modes

**VOX (Voice-Activated):**
- Audio streams automatically when cosplayer speaks
- Threshold-based detection with hold time
- Best for hands-free operation

**PTT (Push-to-Talk):**
- Requires physical button press on device
- Can use GPIO button or API call
- Best when costume has accessible button

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
      "InputDevice": "default",
      "SampleRate": 48000,
      "BufferSize": 1024,
      "Volume": 0.8,
      "MicMode": "vox",
      "VoxThreshold": 0.02,
      "VoxHoldMs": 500
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
    },
    "Camera": {
      "Enabled": false,
      "Width": 1920,
      "Height": 1080,
      "Framerate": 30,
      "RecordingPath": "/home/familiar/recordings",
      "StreamBitrate": 4000000,
      "RecordingBitrate": 8000000
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
    public string InputDevice { get; set; } = "default";
    public int SampleRate { get; set; } = 48000;
    public int BufferSize { get; set; } = 1024;
    public float Volume { get; set; } = 0.8f;
    public string MicMode { get; set; } = "vox"; // "vox" or "ptt"
    public float VoxThreshold { get; set; } = 0.02f;
    public int VoxHoldMs { get; set; } = 500;
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

## WiFi Access Point Configuration

The Pi hosts its own WiFi network so handlers can connect directly without relying on venue WiFi.

### Network Architecture

```
┌─────────────────┐         ┌─────────────────────────────────┐
│ Handler's Phone │  WiFi   │         Raspberry Pi            │
│                 │◄───────►│                                 │
│  192.168.4.x    │         │  192.168.4.1 (AP)               │
└─────────────────┘         │                                 │
                            │  hostapd  ──► wlan0 (AP mode)   │
                            │  dnsmasq  ──► DHCP server       │
                            │  Familiar ──► Web server :80    │
                            └─────────────────────────────────┘
```

### hostapd Configuration

```ini
# /etc/hostapd/hostapd.conf
interface=wlan0
driver=nl80211
ssid=Familiar
hw_mode=g
channel=7
wmm_enabled=0
macaddr_acl=0
auth_algs=1
ignore_broadcast_ssid=0
wpa=2
wpa_passphrase=YourSecurePassword
wpa_key_mgmt=WPA-PSK
wpa_pairwise=TKIP
rsn_pairwise=CCMP
```

Enable hostapd:
```bash
sudo systemctl unmask hostapd
sudo systemctl enable hostapd
```

### dnsmasq Configuration

```ini
# /etc/dnsmasq.conf
interface=wlan0
dhcp-range=192.168.4.2,192.168.4.20,255.255.255.0,24h
domain=local
address=/familiar.local/192.168.4.1
```

### Static IP Configuration

```ini
# /etc/dhcpcd.conf (append)
interface wlan0
    static ip_address=192.168.4.1/24
    nohook wpa_supplicant
```

### AP Setup Script

```bash
#!/bin/bash
# scripts/setup-ap.sh

set -e

echo "Installing AP packages..."
sudo apt-get update
sudo apt-get install -y hostapd dnsmasq

echo "Stopping services during config..."
sudo systemctl stop hostapd
sudo systemctl stop dnsmasq

echo "Configuring static IP..."
sudo tee -a /etc/dhcpcd.conf > /dev/null <<EOF

interface wlan0
    static ip_address=192.168.4.1/24
    nohook wpa_supplicant
EOF

echo "Configuring hostapd..."
sudo tee /etc/hostapd/hostapd.conf > /dev/null <<EOF
interface=wlan0
driver=nl80211
ssid=Familiar
hw_mode=g
channel=7
wmm_enabled=0
macaddr_acl=0
auth_algs=1
ignore_broadcast_ssid=0
wpa=2
wpa_passphrase=ChangeMeNow123
wpa_key_mgmt=WPA-PSK
wpa_pairwise=TKIP
rsn_pairwise=CCMP
EOF

echo "Pointing to hostapd config..."
sudo sed -i 's|#DAEMON_CONF=""|DAEMON_CONF="/etc/hostapd/hostapd.conf"|' /etc/default/hostapd

echo "Configuring dnsmasq..."
sudo mv /etc/dnsmasq.conf /etc/dnsmasq.conf.orig
sudo tee /etc/dnsmasq.conf > /dev/null <<EOF
interface=wlan0
dhcp-range=192.168.4.2,192.168.4.20,255.255.255.0,24h
domain=local
address=/familiar.local/192.168.4.1
EOF

echo "Enabling services..."
sudo systemctl unmask hostapd
sudo systemctl enable hostapd
sudo systemctl enable dnsmasq

echo "AP setup complete! Reboot to activate."
echo "SSID: Familiar"
echo "Password: ChangeMeNow123 (change this in /etc/hostapd/hostapd.conf)"
echo "Web UI: http://192.168.4.1"
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

echo "========================================="
echo "Familiar Firmware Setup"
echo "========================================="

# Install system dependencies
echo "[1/6] Installing system dependencies..."
sudo apt-get update
sudo apt-get install -y espeak alsa-utils hostapd dnsmasq

# Install .NET 8 Runtime
echo "[2/6] Installing .NET 8 Runtime..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime aspnetcore
export PATH="$HOME/.dotnet:$PATH"

# Configure WiFi Access Point
echo "[3/6] Configuring WiFi Access Point..."
sudo systemctl stop hostapd 2>/dev/null || true
sudo systemctl stop dnsmasq 2>/dev/null || true

# Static IP for wlan0
if ! grep -q "interface wlan0" /etc/dhcpcd.conf; then
    sudo tee -a /etc/dhcpcd.conf > /dev/null <<EOF

interface wlan0
    static ip_address=192.168.4.1/24
    nohook wpa_supplicant
EOF
fi

# hostapd config
sudo tee /etc/hostapd/hostapd.conf > /dev/null <<EOF
interface=wlan0
driver=nl80211
ssid=Familiar
hw_mode=g
channel=7
wmm_enabled=0
macaddr_acl=0
auth_algs=1
ignore_broadcast_ssid=0
wpa=2
wpa_passphrase=FamiliarDevice
wpa_key_mgmt=WPA-PSK
wpa_pairwise=TKIP
rsn_pairwise=CCMP
EOF

sudo sed -i 's|#DAEMON_CONF=""|DAEMON_CONF="/etc/hostapd/hostapd.conf"|' /etc/default/hostapd

# dnsmasq config
sudo mv /etc/dnsmasq.conf /etc/dnsmasq.conf.orig 2>/dev/null || true
sudo tee /etc/dnsmasq.conf > /dev/null <<EOF
interface=wlan0
dhcp-range=192.168.4.2,192.168.4.20,255.255.255.0,24h
domain=local
address=/familiar.local/192.168.4.1
EOF

sudo systemctl unmask hostapd
sudo systemctl enable hostapd
sudo systemctl enable dnsmasq

# Build application
echo "[4/6] Building Familiar application..."
dotnet publish src/Familiar.Host -c Release -o /opt/familiar

# Create service user
echo "[5/6] Creating service user..."
sudo useradd -r -s /bin/false familiar 2>/dev/null || true
sudo chown -R familiar:familiar /opt/familiar
sudo usermod -a -G audio,dialout familiar

# Install systemd service
echo "[6/6] Installing systemd service..."
sudo cp scripts/familiar.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable familiar

echo ""
echo "========================================="
echo "Setup complete!"
echo "========================================="
echo ""
echo "WiFi Network:"
echo "  SSID:     Familiar"
echo "  Password: FamiliarDevice"
echo "  Web UI:   http://192.168.4.1"
echo ""
echo "Next steps:"
echo "  1. sudo reboot"
echo "  2. Connect phone to 'Familiar' WiFi"
echo "  3. Open http://192.168.4.1"
echo ""
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
| `/api/mic/status` | GET | Microphone capture status |
| `/api/mic/start` | POST | Start voice capture |
| `/api/mic/stop` | POST | Stop voice capture |
| `/api/mic/mode` | POST | Set mode: "vox" (voice-activated) or "ptt" |

### Camera Endpoints (Pi 5 Only)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/camera/status` | GET | Camera availability and state |
| `/api/camera/snapshot` | GET | Capture and return JPEG image |
| `/api/camera/recording/start` | POST | Start recording to file |
| `/api/camera/recording/stop` | POST | Stop recording, return filename |
| `/api/camera/recordings` | GET | List saved recordings |
| `/api/camera/recordings/{id}` | GET | Download recording file |
| `/api/camera/recordings/{id}` | DELETE | Delete recording |

### WebSocket Endpoints

| Endpoint | Description |
|----------|-------------|
| `/ws/audio/down` | Audio: Handler → Cosplayer (handler sends, Pi plays) |
| `/ws/audio/up` | Audio: Cosplayer → Handler (Pi captures, handler receives) |
| `/ws/video` | Video streaming (Pi 5 only, H.264 frames) |

### Example API Calls

```bash
# Get status
curl http://192.168.4.1/api/status

# Set volume
curl -X POST http://192.168.4.1/api/volume \
  -H "Content-Type: application/json" \
  -d '{"level": 0.75}'

# Send TTS message
curl -X POST http://192.168.4.1/api/tts \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello from the API"}'

# Camera (Pi 5 only)
# Get camera status
curl http://192.168.4.1/api/camera/status

# Capture snapshot
curl http://192.168.4.1/api/camera/snapshot --output snapshot.jpg

# Start recording
curl -X POST http://192.168.4.1/api/camera/recording/start \
  -H "Content-Type: application/json" \
  -d '{"filename": "con-day1-panel"}'

# Stop recording
curl -X POST http://192.168.4.1/api/camera/recording/stop

# List recordings
curl http://192.168.4.1/api/camera/recordings
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
| Camera not detected | Not enabled / wrong Pi | Set `Camera.Enabled: true`, Pi 5 only |
| Camera stream laggy | WiFi bandwidth | Reduce resolution or framerate |
| Recording fails | Disk full / permissions | Check storage, ensure write access |
| Mic not working | Wrong input device | Check `arecord -l`, update InputDevice |
| VOX always transmitting | Threshold too low | Increase VoxThreshold in config |
| VOX not triggering | Threshold too high | Decrease VoxThreshold in config |
| Audio feedback/echo | Mic too close to speaker | Separate mic/speaker, use earpiece |

### Debug Commands

```bash
# Check .NET installation
dotnet --info

# Check audio output devices
aplay -l

# Check audio input devices (microphones)
arecord -l

# Test microphone capture
arecord -D default -f S16_LE -r 48000 -c 1 -d 5 test.wav
aplay test.wav

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

# Check camera (Pi 5 only)
libcamera-hello --list-cameras
libcamera-jpeg -o test.jpg

# Test camera streaming
libcamera-vid -t 10000 --width 1920 --height 1080 -o test.h264
```
