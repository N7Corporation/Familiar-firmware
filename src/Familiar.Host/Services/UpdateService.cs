using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Familiar.Host.Models;
using Familiar.Host.Options;
using Microsoft.Extensions.Options;

namespace Familiar.Host.Services;

public interface IUpdateService
{
    UpdateStatus Status { get; }
    Task<UpdateCheckResponse> CheckForUpdatesAsync(CancellationToken ct = default);
    Task<bool> DownloadUpdateAsync(ReleaseInfo release, CancellationToken ct = default);
    Task<bool> InstallUpdateAsync(CancellationToken ct = default);
}

public class UpdateService : IUpdateService, IHostedService, IDisposable
{
    private readonly UpdateOptions _options;
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly RSA? _publicKey;
    private Timer? _checkTimer;
    private string? _downloadedPackagePath;

    public UpdateStatus Status { get; private set; } = new()
    {
        CurrentVersion = GetCurrentVersion()
    };

    public UpdateService(IOptions<UpdateOptions> options, ILogger<UpdateService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.ServerUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };

        if (!string.IsNullOrEmpty(_options.PublicKey))
        {
            try
            {
                _publicKey = RSA.Create();
                _publicKey.ImportFromPem(_options.PublicKey);
                _logger.LogInformation("Loaded update signing public key");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load public key for update verification");
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Update service is disabled");
            return Task.CompletedTask;
        }

        if (_publicKey == null)
        {
            _logger.LogWarning("Update service enabled but no public key configured - updates will fail signature verification. Configure 'Familiar:Update:PublicKey' with the server's public key.");
        }

        _logger.LogInformation("Update service started, server: {Server}, channel: {Channel}",
            _options.ServerUrl, _options.Channel);

        if (_options.CheckOnStartup)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await CheckForUpdatesAsync(cancellationToken);
            }, cancellationToken);
        }

        if (_options.CheckIntervalMinutes > 0)
        {
            _checkTimer = new Timer(
                async _ => await CheckForUpdatesAsync(CancellationToken.None),
                null,
                TimeSpan.FromMinutes(_options.CheckIntervalMinutes),
                TimeSpan.FromMinutes(_options.CheckIntervalMinutes));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _checkTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public async Task<UpdateCheckResponse> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        Status.State = UpdateState.Checking;
        Status.Error = null;

        try
        {
            var request = new { CurrentVersion = Status.CurrentVersion, Channel = _options.Channel };
            var response = await _httpClient.PostAsJsonAsync("/api/check", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Update check failed with status {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<UpdateCheckResponse>(cancellationToken: ct);
            if (result == null)
            {
                throw new InvalidOperationException("Invalid response from update server");
            }

            Status.LastChecked = DateTime.UtcNow;
            Status.UpdateAvailable = result.UpdateAvailable;
            Status.AvailableUpdate = result.LatestRelease;
            Status.State = UpdateState.Idle;

            if (result.UpdateAvailable && result.LatestRelease != null)
            {
                _logger.LogInformation("Update available: {Version} (current: {Current})",
                    result.LatestRelease.Version, Status.CurrentVersion);

                if (_options.AutoInstall)
                {
                    _ = Task.Run(async () =>
                    {
                        if (await DownloadUpdateAsync(result.LatestRelease, ct))
                        {
                            await InstallUpdateAsync(ct);
                        }
                    }, ct);
                }
            }
            else
            {
                _logger.LogDebug("No updates available");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            Status.State = UpdateState.Failed;
            Status.Error = ex.Message;
            throw;
        }
    }

    public async Task<bool> DownloadUpdateAsync(ReleaseInfo release, CancellationToken ct = default)
    {
        Status.State = UpdateState.Downloading;
        Status.DownloadProgress = 0;
        Status.Error = null;

        try
        {
            _logger.LogInformation("Downloading update {Version} from {Url}",
                release.Version, release.DownloadUrl);

            var tempPath = Path.Combine(Path.GetTempPath(), $"familiar-{release.Version}.tar.gz");

            using var response = await _httpClient.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? release.SizeBytes;
            var buffer = new byte[81920];
            long bytesRead = 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(tempPath);

            int read;
            while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                bytesRead += read;
                Status.DownloadProgress = (int)(bytesRead * 100 / totalBytes);
            }

            _logger.LogInformation("Download complete: {Bytes} bytes", bytesRead);

            // Verify hash
            Status.State = UpdateState.Verifying;
            fileStream.Position = 0;
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(fileStream, ct)).ToLowerInvariant();

            if (hash != release.Sha256Hash.ToLowerInvariant())
            {
                File.Delete(tempPath);
                throw new InvalidOperationException($"Hash mismatch: expected {release.Sha256Hash}, got {hash}");
            }

            _logger.LogInformation("Hash verified: {Hash}", hash);

            // Verify signature - MANDATORY for security
            if (_publicKey == null)
            {
                File.Delete(tempPath);
                throw new InvalidOperationException("Cannot verify update signature: no public key configured. Updates require signature verification for security.");
            }

            var packageBytes = await File.ReadAllBytesAsync(tempPath, ct);
            var signatureBytes = Convert.FromBase64String(release.Signature);

            if (!_publicKey.VerifyData(packageBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                File.Delete(tempPath);
                throw new InvalidOperationException("Signature verification failed - update rejected");
            }

            _logger.LogInformation("Signature verified successfully");

            _downloadedPackagePath = tempPath;
            Status.State = UpdateState.Idle;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download update");
            Status.State = UpdateState.Failed;
            Status.Error = ex.Message;
            return false;
        }
    }

    public async Task<bool> InstallUpdateAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_downloadedPackagePath) || !File.Exists(_downloadedPackagePath))
        {
            Status.Error = "No downloaded update available";
            Status.State = UpdateState.Failed;
            return false;
        }

        Status.State = UpdateState.Installing;
        Status.Error = null;

        try
        {
            _logger.LogInformation("Installing update from {Path}", _downloadedPackagePath);

            // Backup current installation
            if (Directory.Exists(_options.InstallPath))
            {
                if (Directory.Exists(_options.BackupPath))
                {
                    Directory.Delete(_options.BackupPath, true);
                }

                CopyDirectory(_options.InstallPath, _options.BackupPath);
                _logger.LogInformation("Backed up current installation to {Path}", _options.BackupPath);
            }

            // Extract update
            var extractPath = Path.Combine(Path.GetTempPath(), "familiar-update");
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            await ExtractTarGzAsync(_downloadedPackagePath, extractPath, ct);

            // Copy files to install path
            if (!Directory.Exists(_options.InstallPath))
            {
                Directory.CreateDirectory(_options.InstallPath);
            }

            CopyDirectory(extractPath, _options.InstallPath);
            _logger.LogInformation("Installed update to {Path}", _options.InstallPath);

            // Cleanup
            File.Delete(_downloadedPackagePath);
            Directory.Delete(extractPath, true);
            _downloadedPackagePath = null;

            Status.State = UpdateState.RestartRequired;
            _logger.LogInformation("Update installed successfully, restart required");

            // Optionally restart the service
            RestartService();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install update");
            Status.State = UpdateState.Failed;
            Status.Error = ex.Message;

            // Attempt rollback
            if (Directory.Exists(_options.BackupPath))
            {
                try
                {
                    _logger.LogInformation("Rolling back to previous version");
                    CopyDirectory(_options.BackupPath, _options.InstallPath);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Rollback failed");
                }
            }

            return false;
        }
    }

    private static string GetCurrentVersion()
    {
        var assembly = typeof(UpdateService).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "0.0.0";
    }

    private static void CopyDirectory(string source, string destination)
    {
        var dir = new DirectoryInfo(source);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destination, file.Name);
            file.CopyTo(targetPath, true);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var newDestination = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, newDestination);
        }
    }

    private static async Task ExtractTarGzAsync(string archivePath, string destinationPath, CancellationToken ct)
    {
        Directory.CreateDirectory(destinationPath);

        // Use tar command on Linux (more reliable for preserving permissions)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{destinationPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync(ct);
                    throw new InvalidOperationException($"tar extraction failed: {error}");
                }
            }
        }
        else
        {
            // Fallback for other platforms - use GZipStream
            await using var fileStream = File.OpenRead(archivePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

            // Note: This is a simplified extraction that doesn't handle tar format properly
            // For production, consider using a proper tar library like SharpCompress
            throw new PlatformNotSupportedException("Windows extraction not implemented - use SharpCompress package");
        }
    }

    private void RestartService()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                _logger.LogInformation("Requesting service restart...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "restart familiar",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restart service automatically");
            }
        }
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
        _httpClient.Dispose();
        _publicKey?.Dispose();
    }
}
