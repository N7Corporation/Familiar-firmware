using System.Security.Cryptography;
using System.Text.Json;
using Familiar.UpdateServer.Models;
using Familiar.UpdateServer.Options;
using Microsoft.Extensions.Options;

namespace Familiar.UpdateServer.Services;

public interface IReleaseService
{
    Task<ReleaseInfo?> GetLatestReleaseAsync(string channel = "stable");
    Task<ReleaseInfo?> GetReleaseAsync(string version);
    Task<IEnumerable<ReleaseInfo>> GetAllReleasesAsync(string? channel = null);
    Task<ReleaseInfo> CreateReleaseAsync(string version, string channel, Stream packageStream, string? releaseNotes = null, string? minimumVersion = null);
    Task<Stream?> GetPackageStreamAsync(string version);
    string GetPackagePath(string version);
}

public class ReleaseService : IReleaseService
{
    private readonly UpdateServerOptions _options;
    private readonly ISigningService _signingService;
    private readonly ILogger<ReleaseService> _logger;
    private readonly string _manifestPath;
    private List<ReleaseInfo> _releases = new();

    public ReleaseService(
        IOptions<UpdateServerOptions> options,
        ISigningService signingService,
        ILogger<ReleaseService> logger)
    {
        _options = options.Value;
        _signingService = signingService;
        _logger = logger;
        _manifestPath = Path.Combine(_options.ReleasesPath, "manifest.json");

        EnsureDirectoryExists();
        LoadManifest();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_options.ReleasesPath))
        {
            Directory.CreateDirectory(_options.ReleasesPath);
            _logger.LogInformation("Created releases directory at {Path}", _options.ReleasesPath);
        }
    }

    private void LoadManifest()
    {
        if (File.Exists(_manifestPath))
        {
            try
            {
                var json = File.ReadAllText(_manifestPath);
                _releases = JsonSerializer.Deserialize<List<ReleaseInfo>>(json) ?? new List<ReleaseInfo>();
                _logger.LogInformation("Loaded {Count} releases from manifest", _releases.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load manifest from {Path}", _manifestPath);
                _releases = new List<ReleaseInfo>();
            }
        }
    }

    private void SaveManifest()
    {
        var json = JsonSerializer.Serialize(_releases, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_manifestPath, json);
    }

    public Task<ReleaseInfo?> GetLatestReleaseAsync(string channel = "stable")
    {
        var release = _releases
            .Where(r => r.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => Version.Parse(r.Version))
            .FirstOrDefault();

        return Task.FromResult(release);
    }

    public Task<ReleaseInfo?> GetReleaseAsync(string version)
    {
        var release = _releases.FirstOrDefault(r => r.Version == version);
        return Task.FromResult(release);
    }

    public Task<IEnumerable<ReleaseInfo>> GetAllReleasesAsync(string? channel = null)
    {
        IEnumerable<ReleaseInfo> releases = _releases;

        if (!string.IsNullOrEmpty(channel))
        {
            releases = releases.Where(r => r.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IEnumerable<ReleaseInfo>>(releases.OrderByDescending(r => Version.Parse(r.Version)).ToList());
    }

    public async Task<ReleaseInfo> CreateReleaseAsync(
        string version,
        string channel,
        Stream packageStream,
        string? releaseNotes = null,
        string? minimumVersion = null)
    {
        var packageFileName = $"familiar-{version}.tar.gz";
        var packagePath = Path.Combine(_options.ReleasesPath, packageFileName);

        // Save the package file
        using (var fileStream = File.Create(packagePath))
        {
            await packageStream.CopyToAsync(fileStream);
        }

        // Calculate SHA256 hash
        var packageBytes = await File.ReadAllBytesAsync(packagePath);
        var sha256Hash = Convert.ToHexString(SHA256.HashData(packageBytes)).ToLowerInvariant();

        // Sign the package
        var signature = _signingService.SignData(packageBytes);

        var release = new ReleaseInfo
        {
            Version = version,
            Channel = channel,
            ReleasedAt = DateTime.UtcNow,
            DownloadUrl = $"{_options.BaseDownloadUrl}/api/releases/{version}/download",
            SizeBytes = packageBytes.Length,
            Sha256Hash = sha256Hash,
            Signature = signature,
            ReleaseNotes = releaseNotes,
            MinimumVersion = minimumVersion
        };

        // Remove existing release with same version if exists
        _releases.RemoveAll(r => r.Version == version);
        _releases.Add(release);
        SaveManifest();

        _logger.LogInformation("Created release {Version} on channel {Channel}", version, channel);

        return release;
    }

    public Task<Stream?> GetPackageStreamAsync(string version)
    {
        var packagePath = GetPackagePath(version);

        if (!File.Exists(packagePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(packagePath);
        return Task.FromResult<Stream?>(stream);
    }

    public string GetPackagePath(string version)
    {
        return Path.Combine(_options.ReleasesPath, $"familiar-{version}.tar.gz");
    }
}
