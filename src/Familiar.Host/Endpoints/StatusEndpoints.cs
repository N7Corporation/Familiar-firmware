using Familiar.Audio;
using Familiar.Camera;
using Familiar.Meshtastic;
using Familiar.Tts;

namespace Familiar.Host.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        app.MapGet("/api/status", (
            IAudioManager audio,
            ITtsEngine tts,
            MeshtasticService meshtastic,
            ICameraService camera) =>
        {
            return Results.Ok(new
            {
                Status = "Online",
                Version = "0.1.0",
                Audio = new
                {
                    Volume = audio.Volume,
                    Muted = audio.IsMuted,
                    Capturing = audio.IsCapturing,
                    MicMode = audio.MicMode
                },
                Tts = new
                {
                    Available = tts.IsAvailable
                },
                Meshtastic = new
                {
                    Connected = meshtastic.Client.IsConnected,
                    NodeCount = meshtastic.Client.KnownNodes.Count
                },
                Camera = new
                {
                    Available = camera.IsAvailable,
                    Streaming = camera.IsStreaming,
                    Recording = camera.IsRecording
                }
            });
        })
        .WithName("GetStatus");

        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy" }))
            .WithName("HealthCheck");
    }
}
