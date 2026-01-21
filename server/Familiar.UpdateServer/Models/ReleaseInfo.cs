namespace Familiar.UpdateServer.Models;

public class ReleaseInfo
{
    public required string Version { get; set; }
    public required string Channel { get; set; }
    public required DateTime ReleasedAt { get; set; }
    public required string DownloadUrl { get; set; }
    public required long SizeBytes { get; set; }
    public required string Sha256Hash { get; set; }
    public required string Signature { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? MinimumVersion { get; set; }
}

public class VersionManifest
{
    public required string LatestVersion { get; set; }
    public required string Channel { get; set; }
    public required DateTime CheckedAt { get; set; }
    public required ReleaseInfo Release { get; set; }
}

public class UpdateCheckRequest
{
    public required string CurrentVersion { get; set; }
    public string Channel { get; set; } = "stable";
}

public class UpdateCheckResponse
{
    public bool UpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public ReleaseInfo? LatestRelease { get; set; }
}
