namespace Familiar.UpdateServer.Options;

public class UpdateServerOptions
{
    public const string SectionName = "UpdateServer";

    public string ReleasesPath { get; set; } = "./releases";
    public string PrivateKeyPath { get; set; } = "./keys/private.pem";
    public string PublicKeyPath { get; set; } = "./keys/public.pem";
    public string BaseDownloadUrl { get; set; } = "https://familiar.n7co.com";
}
