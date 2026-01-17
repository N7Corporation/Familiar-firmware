using Familiar.Audio;
using Familiar.Camera;
using Familiar.Host.Endpoints;
using Familiar.Host.WebSockets;
using Familiar.Meshtastic;
using Familiar.Tts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "/var/log/familiar/familiar-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
builder.Services.Configure<AudioOptions>(
    builder.Configuration.GetSection(AudioOptions.SectionName));
builder.Services.Configure<TtsOptions>(
    builder.Configuration.GetSection(TtsOptions.SectionName));
builder.Services.Configure<MeshtasticOptions>(
    builder.Configuration.GetSection(MeshtasticOptions.SectionName));
builder.Services.Configure<CameraOptions>(
    builder.Configuration.GetSection(CameraOptions.SectionName));

// Add services
builder.Services.AddFamiliarAudio();
builder.Services.AddFamiliarTts();
builder.Services.AddFamiliarMeshtastic();
builder.Services.AddFamiliarCamera();

// WebSocket handlers
builder.Services.AddSingleton<AudioDownlinkHandler>();
builder.Services.AddSingleton<AudioUplinkHandler>();
builder.Services.AddSingleton<VideoStreamHandler>();

var app = builder.Build();

// Initialize services
await InitializeServicesAsync(app.Services);

// Static files for web UI
app.UseDefaultFiles();
app.UseStaticFiles();

// WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// WebSocket endpoints
app.Map("/ws/audio/down", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var handler = context.RequestServices.GetRequiredService<AudioDownlinkHandler>();
        await handler.HandleAsync(context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Map("/ws/audio/up", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var handler = context.RequestServices.GetRequiredService<AudioUplinkHandler>();
        await handler.HandleAsync(context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Map("/ws/video", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var handler = context.RequestServices.GetRequiredService<VideoStreamHandler>();
        await handler.HandleAsync(context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

// API endpoints
app.MapStatusEndpoints();
app.MapAudioEndpoints();
app.MapTtsEndpoints();
app.MapMeshtasticEndpoints();
app.MapCameraEndpoints();

try
{
    Log.Information("Starting Familiar Firmware...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeServicesAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Wire up TTS to Audio Manager
    var audioManager = services.GetRequiredService<IAudioManager>();
    var ttsEngine = services.GetRequiredService<ITtsEngine>();

    if (audioManager is AudioManager am)
    {
        am.SetTtsCallback(async (text, ct) => await ttsEngine.SynthesizeAsync(text, ct));
        logger.LogInformation("TTS engine connected to audio manager");
    }

    // Wire up Meshtastic to TTS
    var meshtasticService = services.GetRequiredService<MeshtasticService>();
    meshtasticService.TextMessageReceived += async (text, fromNode) =>
    {
        logger.LogInformation("Speaking message from {Node}: {Text}", fromNode, text);
        await audioManager.PlayTtsAsync(text);
    };

    meshtasticService.CommandReceived += async (command, fromNode) =>
    {
        await HandleMeshtasticCommand(command, fromNode, audioManager, logger);
    };

    logger.LogInformation("Services initialized");
}

static async Task HandleMeshtasticCommand(
    string command,
    string fromNode,
    IAudioManager audioManager,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var parts = command.Split(' ', 2, StringSplitOptions.TrimEntries);
    var cmd = parts[0].ToLowerInvariant();
    var args = parts.Length > 1 ? parts[1] : string.Empty;

    switch (cmd)
    {
        case "!vol":
            if (int.TryParse(args, out var volume))
            {
                audioManager.SetVolume(volume / 100f);
                logger.LogInformation("Volume set to {Volume}% via Meshtastic", volume);
            }
            break;

        case "!mute":
            audioManager.IsMuted = true;
            logger.LogInformation("Audio muted via Meshtastic");
            break;

        case "!unmute":
            audioManager.IsMuted = false;
            logger.LogInformation("Audio unmuted via Meshtastic");
            break;
    }
}
