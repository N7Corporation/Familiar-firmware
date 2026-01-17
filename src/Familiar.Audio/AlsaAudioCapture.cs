using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Audio;

/// <summary>
/// Audio capture implementation using ALSA via arecord command.
/// </summary>
public class AlsaAudioCapture : IAudioCapture
{
    private readonly AudioOptions _options;
    private readonly ILogger<AlsaAudioCapture> _logger;
    private readonly Channel<byte[]> _frameChannel;
    private Process? _arecordProcess;
    private Task? _readerTask;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public AlsaAudioCapture(
        IOptions<AudioOptions> options,
        ILogger<AlsaAudioCapture> logger)
    {
        _options = options.Value;
        _logger = logger;
        _frameChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    public bool IsCapturing => _arecordProcess?.HasExited == false;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_arecordProcess != null)
        {
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Start arecord process for continuous capture
        var args = $"-D {_options.InputDevice} -f S16_LE -r {_options.SampleRate} -c 1 -t raw -q -";

        _arecordProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "arecord",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _arecordProcess.Start();
        _logger.LogInformation("Audio capture started on device {Device}", _options.InputDevice);

        // Start background task to read audio data from arecord
        _readerTask = ReadFromProcessAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_arecordProcess != null && !_arecordProcess.HasExited)
        {
            try
            {
                _arecordProcess.Kill();
                await _arecordProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping arecord process");
            }
        }

        _arecordProcess?.Dispose();
        _arecordProcess = null;

        if (_readerTask != null)
        {
            try
            {
                await _readerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _logger.LogInformation("Audio capture stopped");
    }

    public async IAsyncEnumerable<byte[]> ReadFramesAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var frame in _frameChannel.Reader.ReadAllAsync(ct))
        {
            yield return frame;
        }
    }

    private async Task ReadFromProcessAsync(CancellationToken ct)
    {
        var buffer = new byte[_options.BufferSize];

        try
        {
            while (!ct.IsCancellationRequested && _arecordProcess?.HasExited == false)
            {
                var bytesRead = await _arecordProcess.StandardOutput.BaseStream.ReadAsync(
                    buffer, 0, buffer.Length, ct);

                if (bytesRead > 0)
                {
                    var frame = new byte[bytesRead];
                    Array.Copy(buffer, frame, bytesRead);
                    await _frameChannel.Writer.WriteAsync(frame, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from arecord process");
        }
        finally
        {
            _frameChannel.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();

        if (_arecordProcess != null && !_arecordProcess.HasExited)
        {
            try
            {
                _arecordProcess.Kill();
            }
            catch
            {
                // Ignore
            }
        }

        _arecordProcess?.Dispose();
        _cts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
