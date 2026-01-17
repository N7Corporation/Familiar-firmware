using Familiar.Camera;

namespace Familiar.Host.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/camera");

        group.MapGet("/status", (ICameraService camera) =>
        {
            return Results.Ok(new
            {
                Available = camera.IsAvailable,
                Streaming = camera.IsStreaming,
                Recording = camera.IsRecording,
                CurrentRecording = camera.CurrentRecordingFile
            });
        });

        group.MapGet("/snapshot", async (ICameraService camera, CancellationToken ct) =>
        {
            if (!camera.IsAvailable)
            {
                return Results.BadRequest(new { Error = "Camera not available" });
            }

            var data = await camera.CaptureSnapshotAsync(ct);
            if (data == null)
            {
                return Results.StatusCode(500);
            }

            return Results.File(data, "image/jpeg", "snapshot.jpg");
        });

        group.MapPost("/stream/start", async (ICameraService camera, CancellationToken ct) =>
        {
            if (!camera.IsAvailable)
            {
                return Results.BadRequest(new { Error = "Camera not available" });
            }

            var success = await camera.StartStreamingAsync(ct);
            return Results.Ok(new { Streaming = success });
        });

        group.MapPost("/stream/stop", async (ICameraService camera) =>
        {
            await camera.StopStreamingAsync();
            return Results.Ok(new { Streaming = false });
        });

        group.MapPost("/recording/start", async (
            RecordingRequest request,
            ICameraService camera,
            CancellationToken ct) =>
        {
            if (!camera.IsAvailable)
            {
                return Results.BadRequest(new { Error = "Camera not available" });
            }

            if (string.IsNullOrWhiteSpace(request.Filename))
            {
                return Results.BadRequest(new { Error = "Filename is required" });
            }

            var success = await camera.StartRecordingAsync(request.Filename, ct);
            return Results.Ok(new
            {
                Recording = success,
                File = camera.CurrentRecordingFile
            });
        });

        group.MapPost("/recording/stop", async (ICameraService camera) =>
        {
            var file = await camera.StopRecordingAsync();
            return Results.Ok(new
            {
                Recording = false,
                File = file
            });
        });

        group.MapGet("/recordings", async (ICameraService camera) =>
        {
            var recordings = await camera.GetRecordingsAsync();
            return Results.Ok(new
            {
                Recordings = recordings.Select(r => new
                {
                    r.Filename,
                    r.SizeBytes,
                    r.CreatedAt
                })
            });
        });

        group.MapGet("/recordings/{filename}", async (
            string filename,
            ICameraService camera) =>
        {
            var recordings = await camera.GetRecordingsAsync();
            var recording = recordings.FirstOrDefault(r => r.Filename == filename);

            if (recording == null)
            {
                return Results.NotFound();
            }

            var data = await File.ReadAllBytesAsync(recording.FullPath);
            var contentType = recording.Filename.EndsWith(".mp4")
                ? "video/mp4"
                : "video/h264";

            return Results.File(data, contentType, recording.Filename);
        });

        group.MapDelete("/recordings/{filename}", async (
            string filename,
            ICameraService camera) =>
        {
            var success = await camera.DeleteRecordingAsync(filename);
            return success ? Results.Ok() : Results.NotFound();
        });
    }

    public record RecordingRequest(string Filename);
}
