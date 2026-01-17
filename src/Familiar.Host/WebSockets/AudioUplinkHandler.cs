using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Familiar.Audio;

namespace Familiar.Host.WebSockets;

/// <summary>
/// Handles audio streaming from cosplayer to handler (uplink).
/// Pi microphone → Handler's phone speaker
/// </summary>
public class AudioUplinkHandler
{
    private readonly IAudioManager _audioManager;
    private readonly ILogger<AudioUplinkHandler> _logger;

    public AudioUplinkHandler(
        IAudioManager audioManager,
        ILogger<AudioUplinkHandler> logger)
    {
        _audioManager = audioManager;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024];
        var subscribed = false;
        Task? streamTask = null;
        var streamCts = new CancellationTokenSource();

        _logger.LogInformation("Audio uplink connected from {IP}",
            context.Connection.RemoteIpAddress);

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = await HandleControlMessage(message, ws, streamCts, context.RequestAborted);

                    if (response == "subscribe" && !subscribed)
                    {
                        subscribed = true;
                        // Start capture if not already capturing
                        if (!_audioManager.IsCapturing)
                        {
                            await _audioManager.StartCaptureAsync(context.RequestAborted);
                        }

                        // Start streaming audio to this WebSocket
                        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            streamCts.Token, context.RequestAborted);
                        streamTask = StreamAudioAsync(ws, linkedCts.Token);
                    }
                    else if (response == "unsubscribe" && subscribed)
                    {
                        subscribed = false;
                        streamCts.Cancel();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error on audio uplink");
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        finally
        {
            streamCts.Cancel();
            if (streamTask != null)
            {
                try { await streamTask; } catch { }
            }
            streamCts.Dispose();
        }

        _logger.LogInformation("Audio uplink disconnected");
    }

    private async Task<string?> HandleControlMessage(
        string message,
        WebSocket ws,
        CancellationTokenSource streamCts,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "subscribe":
                    _logger.LogDebug("Audio uplink subscribed");
                    await SendJsonAsync(ws, new
                    {
                        type = "subscribed",
                        mode = _audioManager.MicMode
                    }, ct);
                    return "subscribe";

                case "unsubscribe":
                    _logger.LogDebug("Audio uplink unsubscribed");
                    await SendJsonAsync(ws, new { type = "unsubscribed" }, ct);
                    return "unsubscribe";

                case "ptt":
                    if (doc.RootElement.TryGetProperty("active", out var activeProp))
                    {
                        _audioManager.PttActive = activeProp.GetBoolean();
                        _logger.LogDebug("PTT active: {Active}", _audioManager.PttActive);
                    }
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid control message: {Message}", message);
        }

        return null;
    }

    private async Task StreamAudioAsync(WebSocket ws, CancellationToken ct)
    {
        bool wasVoiceActive = false;

        try
        {
            await foreach (var frame in _audioManager.GetCapturedAudioAsync(ct))
            {
                if (ws.State != WebSocketState.Open)
                    break;

                // Send VOX state changes
                // Note: This is a simplified implementation
                // Real implementation would track VAD state separately

                // Send audio frame
                await ws.SendAsync(frame, WebSocketMessageType.Binary, true, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming audio uplink");
        }
    }

    private static async Task SendJsonAsync(WebSocket ws, object data, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data);
        await ws.SendAsync(json, WebSocketMessageType.Text, true, ct);
    }
}
