using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Familiar.Audio;
using Familiar.Host.Services;

namespace Familiar.Host.WebSockets;

/// <summary>
/// Handles audio streaming from handler to cosplayer (downlink).
/// Handler's phone → Pi speaker
/// </summary>
public class AudioDownlinkHandler
{
    private readonly IAudioManager _audioManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AudioDownlinkHandler> _logger;

    public AudioDownlinkHandler(
        IAudioManager audioManager,
        ITokenService tokenService,
        ILogger<AudioDownlinkHandler> logger)
    {
        _audioManager = audioManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        // Validate token from query string
        if (!context.Request.Query.TryGetValue("access_token", out var token) ||
            !_tokenService.ValidateToken(token!))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[8192];

        _logger.LogInformation("Audio downlink connected from {IP}",
            context.Connection.RemoteIpAddress);

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Audio data from handler - play it
                    var audioData = buffer.AsMemory(0, result.Count);
                    await _audioManager.PlayStreamAsync(audioData, context.RequestAborted);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Control message
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleControlMessage(ws, message, context.RequestAborted);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error on audio downlink");
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }

        _logger.LogInformation("Audio downlink disconnected");
    }

    private async Task HandleControlMessage(
        WebSocket ws,
        string message,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "start":
                    _logger.LogDebug("Audio downlink stream started");
                    await SendJsonAsync(ws, new { type = "ready" }, ct);
                    break;

                case "stop":
                    _logger.LogDebug("Audio downlink stream stopped");
                    await SendJsonAsync(ws, new { type = "stopped" }, ct);
                    break;

                case "volume":
                    if (doc.RootElement.TryGetProperty("level", out var levelProp))
                    {
                        var level = levelProp.GetSingle();
                        _audioManager.SetVolume(level);
                        await SendJsonAsync(ws, new { type = "volume", level = _audioManager.Volume }, ct);
                    }
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid control message: {Message}", message);
        }
    }

    private static async Task SendJsonAsync(WebSocket ws, object data, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data);
        await ws.SendAsync(json, WebSocketMessageType.Text, true, ct);
    }
}
