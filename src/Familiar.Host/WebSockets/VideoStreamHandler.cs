using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Familiar.Camera;
using Familiar.Host.Services;

namespace Familiar.Host.WebSockets;

/// <summary>
/// Handles video streaming from Pi camera to handler (Pi 5 only).
/// </summary>
public class VideoStreamHandler
{
    private readonly ICameraService _cameraService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<VideoStreamHandler> _logger;

    public VideoStreamHandler(
        ICameraService cameraService,
        ITokenService tokenService,
        ILogger<VideoStreamHandler> logger)
    {
        _cameraService = cameraService;
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

        if (!_cameraService.IsAvailable)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Camera not available");
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024];
        var subscribed = false;
        Task? streamTask = null;
        var streamCts = new CancellationTokenSource();

        _logger.LogInformation("Video stream connected from {IP}",
            context.Connection.RemoteIpAddress);

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = await HandleControlMessage(message, ws, context.RequestAborted);

                    if (response == "subscribe" && !subscribed)
                    {
                        subscribed = true;

                        // Start streaming if not already
                        if (!_cameraService.IsStreaming)
                        {
                            await _cameraService.StartStreamingAsync(context.RequestAborted);
                        }

                        // Start sending frames to this WebSocket
                        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            streamCts.Token, context.RequestAborted);
                        streamTask = StreamVideoAsync(ws, linkedCts.Token);
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
            _logger.LogWarning(ex, "WebSocket error on video stream");
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

        _logger.LogInformation("Video stream disconnected");
    }

    private async Task<string?> HandleControlMessage(
        string message,
        WebSocket ws,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "subscribe":
                    _logger.LogDebug("Video stream subscribed");
                    await SendJsonAsync(ws, new
                    {
                        type = "subscribed",
                        streaming = _cameraService.IsStreaming
                    }, ct);
                    return "subscribe";

                case "unsubscribe":
                    _logger.LogDebug("Video stream unsubscribed");
                    await SendJsonAsync(ws, new { type = "unsubscribed" }, ct);
                    return "unsubscribe";

                case "snapshot":
                    var snapshot = await _cameraService.CaptureSnapshotAsync(ct);
                    if (snapshot != null)
                    {
                        await ws.SendAsync(snapshot, WebSocketMessageType.Binary, true, ct);
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

    private async Task StreamVideoAsync(WebSocket ws, CancellationToken ct)
    {
        try
        {
            await foreach (var frame in _cameraService.GetStreamFramesAsync(ct))
            {
                if (ws.State != WebSocketState.Open)
                    break;

                await ws.SendAsync(frame, WebSocketMessageType.Binary, true, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming video");
        }
    }

    private static async Task SendJsonAsync(WebSocket ws, object data, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data);
        await ws.SendAsync(json, WebSocketMessageType.Text, true, ct);
    }
}
