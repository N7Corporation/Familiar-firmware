namespace Familiar.Host.Models;

public class ReleaseInfo
{
    public string Version { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime ReleasedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public string? MinimumVersion { get; set; }
}

public class UpdateCheckResponse
{
    public bool UpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public ReleaseInfo? LatestRelease { get; set; }
}

public class UpdateStatus
{
    public string CurrentVersion { get; set; } = string.Empty;
    public bool UpdateAvailable { get; set; }
    public ReleaseInfo? AvailableUpdate { get; set; }
    public DateTime? LastChecked { get; set; }
    public UpdateState State { get; set; } = UpdateState.Idle;
    public string? Error { get; set; }
    public int DownloadProgress { get; set; }
}

public enum UpdateState
{
    Idle,
    Checking,
    Downloading,
    Verifying,
    Installing,
    RestartRequired,
    Failed
}
