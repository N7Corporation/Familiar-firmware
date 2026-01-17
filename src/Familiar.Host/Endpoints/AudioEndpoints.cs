using Familiar.Audio;

namespace Familiar.Host.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audio");

        group.MapGet("/status", (IAudioManager audio) =>
        {
            return Results.Ok(new
            {
                Volume = audio.Volume,
                Muted = audio.IsMuted,
                Capturing = audio.IsCapturing,
                MicMode = audio.MicMode,
                PttActive = audio.PttActive
            });
        });

        group.MapPost("/volume", async (VolumeRequest request, IAudioManager audio) =>
        {
            audio.SetVolume(request.Level);
            return Results.Ok(new { Volume = audio.Volume });
        });

        group.MapPost("/mute", (IAudioManager audio) =>
        {
            audio.IsMuted = true;
            return Results.Ok(new { Muted = true });
        });

        group.MapPost("/unmute", (IAudioManager audio) =>
        {
            audio.IsMuted = false;
            return Results.Ok(new { Muted = false });
        });

        group.MapGet("/mic/status", (IAudioManager audio) =>
        {
            return Results.Ok(new
            {
                Capturing = audio.IsCapturing,
                Mode = audio.MicMode,
                PttActive = audio.PttActive
            });
        });

        group.MapPost("/mic/start", async (IAudioManager audio, CancellationToken ct) =>
        {
            await audio.StartCaptureAsync(ct);
            return Results.Ok(new { Capturing = true });
        });

        group.MapPost("/mic/stop", async (IAudioManager audio) =>
        {
            await audio.StopCaptureAsync();
            return Results.Ok(new { Capturing = false });
        });

        group.MapPost("/mic/mode", (MicModeRequest request, IAudioManager audio) =>
        {
            audio.MicMode = request.Mode;
            return Results.Ok(new { Mode = audio.MicMode });
        });

        group.MapPost("/mic/ptt", (PttRequest request, IAudioManager audio) =>
        {
            audio.PttActive = request.Active;
            return Results.Ok(new { PttActive = audio.PttActive });
        });
    }

    public record VolumeRequest(float Level);
    public record MicModeRequest(string Mode);
    public record PttRequest(bool Active);
}
