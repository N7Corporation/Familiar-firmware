using Familiar.Host.Services;

namespace Familiar.Host.Endpoints;

public static class UpdateEndpoints
{
    public static void MapUpdateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/update")
            .RequireAuthorization()
            .RequireRateLimiting("default");

        group.MapGet("/status", (IUpdateService updateService) =>
        {
            return Results.Ok(updateService.Status);
        })
        .WithName("GetUpdateStatus");

        group.MapPost("/check", async (IUpdateService updateService, CancellationToken ct) =>
        {
            try
            {
                var result = await updateService.CheckForUpdatesAsync(ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .WithName("CheckForUpdates");

        group.MapPost("/download", async (IUpdateService updateService, CancellationToken ct) =>
        {
            var status = updateService.Status;

            if (!status.UpdateAvailable || status.AvailableUpdate == null)
            {
                return Results.BadRequest(new { Error = "No update available to download" });
            }

            var success = await updateService.DownloadUpdateAsync(status.AvailableUpdate, ct);
            return success
                ? Results.Ok(new { Message = "Update downloaded successfully" })
                : Results.BadRequest(new { Error = updateService.Status.Error });
        })
        .WithName("DownloadUpdate");

        group.MapPost("/install", async (IUpdateService updateService, CancellationToken ct) =>
        {
            var success = await updateService.InstallUpdateAsync(ct);
            return success
                ? Results.Ok(new { Message = "Update installed, restart required" })
                : Results.BadRequest(new { Error = updateService.Status.Error });
        })
        .WithName("InstallUpdate");
    }
}
