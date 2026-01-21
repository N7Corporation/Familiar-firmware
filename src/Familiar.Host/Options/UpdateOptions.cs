namespace Familiar.Host.Options;

public class UpdateOptions
{
    public const string SectionName = "Familiar:Update";

    public bool Enabled { get; set; } = true;
    public string ServerUrl { get; set; } = "https://familiar.n7co.com";
    public string Channel { get; set; } = "stable";
    public bool CheckOnStartup { get; set; } = true;
    public int CheckIntervalMinutes { get; set; } = 60;
    public bool AutoInstall { get; set; } = false;
    public string InstallPath { get; set; } = "/opt/familiar";
    public string BackupPath { get; set; } = "/opt/familiar-backup";
    public string PublicKey { get; set; } = string.Empty;
}
