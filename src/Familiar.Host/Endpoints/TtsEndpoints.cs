using Familiar.Audio;
using Familiar.Tts;

namespace Familiar.Host.Endpoints;

public static class TtsEndpoints
{
    public static void MapTtsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tts");

        group.MapGet("/status", (ITtsEngine tts) =>
        {
            return Results.Ok(new
            {
                Available = tts.IsAvailable,
                Engine = "espeak"
            });
        });

        group.MapPost("/speak", async (
            SpeakRequest request,
            IAudioManager audio,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new { Error = "Text is required" });
            }

            await audio.PlayTtsAsync(request.Text, request.Priority, ct);
            return Results.Ok(new { Spoken = true });
        });

        group.MapGet("/voices", async (ITtsEngine tts, CancellationToken ct) =>
        {
            var voices = await tts.GetAvailableVoicesAsync(ct);
            return Results.Ok(new { Voices = voices });
        });
    }

    public record SpeakRequest(string Text, int Priority = 0);
}
