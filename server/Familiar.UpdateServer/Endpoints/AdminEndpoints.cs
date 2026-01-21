using Familiar.UpdateServer.Services;

namespace Familiar.UpdateServer.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        // Upload a new release
        group.MapPost("/releases", async (
            HttpRequest request,
            IReleaseService releaseService,
            ILogger<Program> logger) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { Error = "Expected multipart/form-data" });
            }

            var form = await request.ReadFormAsync();

            var version = form["version"].ToString();
            var channel = form["channel"].ToString();
            var releaseNotes = form["releaseNotes"].ToString();
            var minimumVersion = form["minimumVersion"].ToString();
            var packageFile = form.Files.GetFile("package");

            if (string.IsNullOrEmpty(version))
            {
                return Results.BadRequest(new { Error = "Version is required" });
            }

            if (string.IsNullOrEmpty(channel))
            {
                channel = "stable";
            }

            if (packageFile == null || packageFile.Length == 0)
            {
                return Results.BadRequest(new { Error = "Package file is required" });
            }

            try
            {
                using var stream = packageFile.OpenReadStream();
                var release = await releaseService.CreateReleaseAsync(
                    version,
                    channel,
                    stream,
                    string.IsNullOrEmpty(releaseNotes) ? null : releaseNotes,
                    string.IsNullOrEmpty(minimumVersion) ? null : minimumVersion);

                logger.LogInformation("Created release {Version} on channel {Channel}", version, channel);

                return Results.Created($"/api/releases/{version}", release);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create release {Version}", version);
                return Results.StatusCode(500);
            }
        })
        .WithName("CreateRelease")
        .DisableAntiforgery();

        // Generate new key pair (dangerous - invalidates all existing signatures)
        group.MapPost("/generate-keys", (ISigningService signingService, ILogger<Program> logger) =>
        {
            try
            {
                signingService.GenerateKeyPair();
                var publicKey = signingService.GetPublicKey();

                logger.LogWarning("Generated new signing key pair - all existing signatures are now invalid!");

                return Results.Ok(new
                {
                    Message = "New key pair generated. All existing signatures are now invalid.",
                    PublicKey = publicKey
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate key pair");
                return Results.StatusCode(500);
            }
        })
        .WithName("GenerateKeys");

        // Delete a release
        group.MapDelete("/releases/{version}", async (string version, IReleaseService releaseService) =>
        {
            var release = await releaseService.GetReleaseAsync(version);
            if (release == null)
            {
                return Results.NotFound(new { Error = $"Release {version} not found" });
            }

            // Delete the package file
            var packagePath = releaseService.GetPackagePath(version);
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }

            return Results.Ok(new { Message = $"Release {version} deleted" });
        })
        .WithName("DeleteRelease");
    }
}
