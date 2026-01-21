using Familiar.UpdateServer.Models;
using Familiar.UpdateServer.Services;

namespace Familiar.UpdateServer.Endpoints;

public static class UpdateEndpoints
{
    public static void MapUpdateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Check for updates
        group.MapPost("/check", async (UpdateCheckRequest request, IReleaseService releaseService) =>
        {
            var latest = await releaseService.GetLatestReleaseAsync(request.Channel);

            if (latest == null)
            {
                return Results.Ok(new UpdateCheckResponse
                {
                    UpdateAvailable = false,
                    CurrentVersion = request.CurrentVersion
                });
            }

            var currentVersion = Version.Parse(request.CurrentVersion);
            var latestVersion = Version.Parse(latest.Version);
            var updateAvailable = latestVersion > currentVersion;

            // Check minimum version requirement
            if (updateAvailable && !string.IsNullOrEmpty(latest.MinimumVersion))
            {
                var minVersion = Version.Parse(latest.MinimumVersion);
                if (currentVersion < minVersion)
                {
                    // Client version is too old for this update
                    // Could return a different response or chain updates
                }
            }

            return Results.Ok(new UpdateCheckResponse
            {
                UpdateAvailable = updateAvailable,
                CurrentVersion = request.CurrentVersion,
                LatestRelease = updateAvailable ? latest : null
            });
        })
        .WithName("CheckForUpdates");

        // Get version manifest
        group.MapGet("/version", async (string? channel, IReleaseService releaseService) =>
        {
            var channelName = channel ?? "stable";
            var latest = await releaseService.GetLatestReleaseAsync(channelName);

            if (latest == null)
            {
                return Results.NotFound(new { Error = $"No releases found for channel '{channelName}'" });
            }

            return Results.Ok(new VersionManifest
            {
                LatestVersion = latest.Version,
                Channel = channelName,
                CheckedAt = DateTime.UtcNow,
                Release = latest
            });
        })
        .WithName("GetVersionManifest");

        // List all releases
        group.MapGet("/releases", async (string? channel, IReleaseService releaseService) =>
        {
            var releases = await releaseService.GetAllReleasesAsync(channel);
            return Results.Ok(new { Releases = releases });
        })
        .WithName("ListReleases");

        // Get specific release info
        group.MapGet("/releases/{version}", async (string version, IReleaseService releaseService) =>
        {
            var release = await releaseService.GetReleaseAsync(version);

            if (release == null)
            {
                return Results.NotFound(new { Error = $"Release {version} not found" });
            }

            return Results.Ok(release);
        })
        .WithName("GetRelease");

        // Download release package
        group.MapGet("/releases/{version}/download", async (string version, IReleaseService releaseService) =>
        {
            var release = await releaseService.GetReleaseAsync(version);
            if (release == null)
            {
                return Results.NotFound(new { Error = $"Release {version} not found" });
            }

            var stream = await releaseService.GetPackageStreamAsync(version);
            if (stream == null)
            {
                return Results.NotFound(new { Error = $"Package for {version} not found" });
            }

            return Results.File(stream, "application/gzip", $"familiar-{version}.tar.gz");
        })
        .WithName("DownloadRelease");

        // Get public key for signature verification
        group.MapGet("/public-key", (ISigningService signingService) =>
        {
            try
            {
                var publicKey = signingService.GetPublicKey();
                return Results.Ok(new { PublicKey = publicKey });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .WithName("GetPublicKey");
    }
}
