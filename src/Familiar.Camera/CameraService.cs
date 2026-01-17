using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Camera;

/// <summary>
/// Camera service implementation using libcamera tools (Pi 5 only).
/// </summary>
public class CameraService : ICameraService
{
    private readonly CameraOptions _options;
    private readonly ILogger<CameraService> _logger;
    private readonly Channel<byte[]> _frameChannel;

    private Process? _streamProcess;
    private Process? _recordingProcess;
    private Task? _streamReaderTask;
    private CancellationTokenSource? _streamCts;
    private string? _currentRecordingPath;
    private bool _disposed;

    private const string LibCameraVid = "/usr/bin/libcamera-vid";
    private const string LibCameraJpeg = "/usr/bin/libcamera-jpeg";

    public CameraService(
        IOptions<CameraOptions> options,
        ILogger<CameraService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _frameChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(30) // ~1 second buffer at 30fps
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        // Ensure recording directory exists
        if (_options.Enabled && !string.IsNullOrEmpty(_options.RecordingPath))
        {
            Directory.CreateDirectory(_options.RecordingPath);
        }
    }

    public bool IsAvailable =>
        _options.Enabled && File.Exists(LibCameraVid) && File.Exists(LibCameraJpeg);

    public bool IsStreaming => _streamProcess?.HasExited == false;

    public bool IsRecording => _recordingProcess?.HasExited == false;

    public string? CurrentRecordingFile => _currentRecordingPath;

    public async Task<bool> StartStreamingAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Camera not available - libcamera tools not found or camera disabled");
            return false;
        }

        if (IsStreaming)
        {
            _logger.LogWarning("Streaming already active");
            return true;
        }

        _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var args = BuildStreamArgs();

        _streamProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = LibCameraVid,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _streamProcess.Start();
        _streamReaderTask = ReadStreamAsync(_streamProcess.StandardOutput.BaseStream, _streamCts.Token);

        _logger.LogInformation("Camera streaming started at {Width}x{Height}@{Fps}fps",
            _options.Width, _options.Height, _options.Framerate);

        return true;
    }

    public async Task StopStreamingAsync()
    {
        _streamCts?.Cancel();

        if (_streamProcess != null && !_streamProcess.HasExited)
        {
            try
            {
                _streamProcess.Kill();
                await _streamProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping stream process");
            }
        }

        _streamProcess?.Dispose();
        _streamProcess = null;

        if (_streamReaderTask != null)
        {
            try
            {
                await _streamReaderTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _streamCts?.Dispose();
        _streamCts = null;

        _logger.LogInformation("Camera streaming stopped");
    }

    public async Task<bool> StartRecordingAsync(string filename, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Camera not available for recording");
            return false;
        }

        if (IsRecording)
        {
            _logger.LogWarning("Recording already active");
            return false;
        }

        // Sanitize filename
        var safeFilename = SanitizeFilename(filename);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _currentRecordingPath = Path.Combine(
            _options.RecordingPath,
            $"{safeFilename}_{timestamp}.h264");

        var args = BuildRecordingArgs(_currentRecordingPath);

        _recordingProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = LibCameraVid,
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _recordingProcess.Start();

        _logger.LogInformation("Recording started: {Path}", _currentRecordingPath);
        return true;
    }

    public async Task<string?> StopRecordingAsync()
    {
        if (!IsRecording)
        {
            return null;
        }

        var recordedPath = _currentRecordingPath;

        if (_recordingProcess != null && !_recordingProcess.HasExited)
        {
            try
            {
                // Send SIGINT for graceful shutdown
                _recordingProcess.Kill(Signum.SIGINT);
                await _recordingProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping recording process");
                _recordingProcess.Kill();
            }
        }

        _recordingProcess?.Dispose();
        _recordingProcess = null;
        _currentRecordingPath = null;

        _logger.LogInformation("Recording stopped: {Path}", recordedPath);
        return recordedPath;
    }

    public async Task<byte[]?> CaptureSnapshotAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Camera not available for snapshot");
            return null;
        }

        var tempFile = Path.GetTempFileName() + ".jpg";

        try
        {
            var args = $"--width {_options.Width} --height {_options.Height} " +
                       $"-q {_options.SnapshotQuality} -o \"{tempFile}\" -t 1";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = LibCameraJpeg,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0 && File.Exists(tempFile))
            {
                var data = await File.ReadAllBytesAsync(tempFile, ct);
                _logger.LogDebug("Snapshot captured: {Size} bytes", data.Length);
                return data;
            }

            var error = await process.StandardError.ReadToEndAsync(ct);
            _logger.LogError("Snapshot failed: {Error}", error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing snapshot");
            return null;
        }
        finally
        {
            TryDeleteFile(tempFile);
        }
    }

    public async IAsyncEnumerable<byte[]> GetStreamFramesAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var frame in _frameChannel.Reader.ReadAllAsync(ct))
        {
            yield return frame;
        }
    }

    public Task<IReadOnlyList<RecordingInfo>> GetRecordingsAsync()
    {
        var recordings = new List<RecordingInfo>();

        if (Directory.Exists(_options.RecordingPath))
        {
            var files = Directory.GetFiles(_options.RecordingPath, "*.h264")
                .Concat(Directory.GetFiles(_options.RecordingPath, "*.mp4"));

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                recordings.Add(new RecordingInfo
                {
                    Filename = info.Name,
                    FullPath = info.FullName,
                    SizeBytes = info.Length,
                    CreatedAt = info.CreationTimeUtc
                });
            }
        }

        return Task.FromResult<IReadOnlyList<RecordingInfo>>(
            recordings.OrderByDescending(r => r.CreatedAt).ToList());
    }

    public Task<bool> DeleteRecordingAsync(string filename)
    {
        var safeName = Path.GetFileName(filename); // Prevent path traversal
        var fullPath = Path.Combine(_options.RecordingPath, safeName);

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted recording: {Path}", fullPath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete recording: {Path}", fullPath);
            }
        }

        return Task.FromResult(false);
    }

    private string BuildStreamArgs()
    {
        return $"-t 0 --inline " +
               $"--width {_options.Width} --height {_options.Height} " +
               $"--framerate {_options.Framerate} " +
               $"--bitrate {_options.StreamBitrate} " +
               $"--codec h264 -o -";
    }

    private string BuildRecordingArgs(string outputPath)
    {
        return $"-t 0 " +
               $"--width {_options.Width} --height {_options.Height} " +
               $"--framerate {_options.Framerate} " +
               $"--bitrate {_options.RecordingBitrate} " +
               $"--codec h264 -o \"{outputPath}\"";
    }

    private async Task ReadStreamAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[65536]; // 64KB buffer for H.264 frames

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                var frame = new byte[bytesRead];
                Array.Copy(buffer, frame, bytesRead);
                await _frameChannel.Writer.WriteAsync(frame, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading camera stream");
        }
    }

    private static string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopStreamingAsync().GetAwaiter().GetResult();
        StopRecordingAsync().GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
    }
}

// Helper for Linux signals
internal static class Signum
{
    public const int SIGINT = 2;
}

internal static class ProcessExtensions
{
    public static void Kill(this Process process, int signal)
    {
        try
        {
            using var killProcess = Process.Start("kill", $"-{signal} {process.Id}");
            killProcess?.WaitForExit(1000);
        }
        catch
        {
            process.Kill();
        }
    }
}
