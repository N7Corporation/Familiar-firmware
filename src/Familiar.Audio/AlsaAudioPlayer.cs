using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Audio;

/// <summary>
/// Audio player implementation using ALSA via aplay command.
/// </summary>
public class AlsaAudioPlayer : IAudioPlayer
{
    private readonly AudioOptions _options;
    private readonly ILogger<AlsaAudioPlayer> _logger;
    private readonly Channel<byte[]> _playbackChannel;
    private Process? _aplayProcess;
    private Task? _writerTask;
    private CancellationTokenSource? _cts;
    private float _volume = 0.8f;
    private bool _disposed;

    public AlsaAudioPlayer(
        IOptions<AudioOptions> options,
        ILogger<AlsaAudioPlayer> logger)
    {
        _options = options.Value;
        _logger = logger;
        _volume = _options.Volume;
        _playbackChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    public bool IsPlaying => _aplayProcess?.HasExited == false;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_aplayProcess != null)
        {
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Start aplay process for continuous playback
        var args = $"-D {_options.OutputDevice} -f S16_LE -r {_options.SampleRate} -c 1 -t raw -q -";

        _aplayProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "aplay",
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _aplayProcess.Start();
        _logger.LogInformation("Audio player started on device {Device}", _options.OutputDevice);

        // Start background task to write audio data to aplay
        _writerTask = WriteToProcessAsync(_cts.Token);
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default)
    {
        if (_aplayProcess?.HasExited != false)
        {
            await InitializeAsync(ct);
        }

        // Apply volume scaling
        var scaledData = ApplyVolume(audioData.Span);
        await _playbackChannel.Writer.WriteAsync(scaledData, ct);
    }

    public void SetVolume(float level)
    {
        _volume = Math.Clamp(level, 0f, 1f);
        _logger.LogDebug("Volume set to {Volume}", _volume);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_aplayProcess != null && !_aplayProcess.HasExited)
        {
            try
            {
                _aplayProcess.StandardInput.Close();
                await _aplayProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping aplay process");
                _aplayProcess.Kill();
            }
        }

        _aplayProcess?.Dispose();
        _aplayProcess = null;

        if (_writerTask != null)
        {
            try
            {
                await _writerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _logger.LogInformation("Audio player stopped");
    }

    private async Task WriteToProcessAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var data in _playbackChannel.Reader.ReadAllAsync(ct))
            {
                if (_aplayProcess?.HasExited == false)
                {
                    await _aplayProcess.StandardInput.BaseStream.WriteAsync(data, ct);
                    await _aplayProcess.StandardInput.BaseStream.FlushAsync(ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to aplay process");
        }
    }

    private byte[] ApplyVolume(ReadOnlySpan<byte> audioData)
    {
        if (Math.Abs(_volume - 1.0f) < 0.01f)
        {
            return audioData.ToArray();
        }

        var result = new byte[audioData.Length];

        for (int i = 0; i < audioData.Length - 1; i += 2)
        {
            short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
            sample = (short)(sample * _volume);
            result[i] = (byte)(sample & 0xFF);
            result[i + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _aplayProcess?.Dispose();
        _cts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
