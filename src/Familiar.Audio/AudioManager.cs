using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Audio;

/// <summary>
/// Central audio manager that handles both playback and capture operations.
/// </summary>
public class AudioManager : IAudioManager
{
    private readonly IAudioPlayer _player;
    private readonly IAudioCapture _capture;
    private readonly AudioOptions _options;
    private readonly ILogger<AudioManager> _logger;
    private readonly VoiceActivityDetector _vad;
    private readonly Channel<byte[]> _captureOutputChannel;

    private float _volume;
    private bool _isMuted;
    private string _micMode;
    private bool _pttActive;
    private bool _disposed;
    private Task? _vadTask;
    private CancellationTokenSource? _vadCts;
    private bool _isVoiceActive;

    // TTS callback - set by the host to integrate with TTS engine
    private Func<string, CancellationToken, Task<byte[]>>? _ttsCallback;

    /// <summary>
    /// Event raised when voice activity state changes.
    /// </summary>
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityChanged;

    /// <summary>
    /// Gets whether voice is currently active (speaking).
    /// </summary>
    public bool IsVoiceActive => _isVoiceActive;

    public AudioManager(
        IAudioPlayer player,
        IAudioCapture capture,
        IOptions<AudioOptions> options,
        ILogger<AudioManager> logger)
    {
        _player = player;
        _capture = capture;
        _options = options.Value;
        _logger = logger;
        _volume = _options.Volume;
        _micMode = _options.MicMode;
        _vad = new VoiceActivityDetector(_options.VoxThreshold, _options.VoxHoldMs / 20); // Assuming 20ms frames
        _captureOutputChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }

    #region Playback

    public async Task PlayStreamAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default)
    {
        if (_isMuted) return;
        await _player.WriteAsync(audioData, ct);
    }

    public async Task PlayTtsAsync(string text, int priority = 0, CancellationToken ct = default)
    {
        if (_isMuted) return;

        if (_ttsCallback == null)
        {
            _logger.LogWarning("TTS callback not configured, cannot play TTS");
            return;
        }

        var sanitized = SanitizeText(text);
        if (string.IsNullOrWhiteSpace(sanitized)) return;

        try
        {
            var audioData = await _ttsCallback(sanitized, ct);
            if (audioData.Length > 0)
            {
                await _player.WriteAsync(audioData, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing TTS for text: {Text}", text);
        }
    }

    public void SetVolume(float level)
    {
        _volume = Math.Clamp(level, 0f, 1f);
        _player.SetVolume(_volume);
        _logger.LogInformation("Volume set to {Volume:P0}", _volume);
    }

    public float Volume => _volume;

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            _logger.LogInformation("Audio muted: {Muted}", _isMuted);
        }
    }

    #endregion

    #region Capture

    public bool IsCapturing => _capture.IsCapturing;

    public async Task StartCaptureAsync(CancellationToken ct = default)
    {
        await _capture.StartAsync(ct);

        // Start VAD processing task
        _vadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _vadTask = ProcessCaptureWithVadAsync(_vadCts.Token);

        _logger.LogInformation("Audio capture started in {Mode} mode", _micMode);
    }

    public async Task StopCaptureAsync()
    {
        _vadCts?.Cancel();

        if (_vadTask != null)
        {
            try
            {
                await _vadTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        await _capture.StopAsync();
        _logger.LogInformation("Audio capture stopped");
    }

    public async IAsyncEnumerable<byte[]> GetCapturedAudioAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var frame in _captureOutputChannel.Reader.ReadAllAsync(ct))
        {
            yield return frame;
        }
    }

    public string MicMode
    {
        get => _micMode;
        set
        {
            _micMode = value.ToLowerInvariant() switch
            {
                "vox" => "vox",
                "ptt" => "ptt",
                _ => "vox"
            };
            _logger.LogInformation("Mic mode set to {Mode}", _micMode);
        }
    }

    public bool PttActive
    {
        get => _pttActive;
        set
        {
            _pttActive = value;
            if (_micMode == "ptt")
            {
                _logger.LogDebug("PTT active: {Active}", _pttActive);
            }
        }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Sets the TTS callback function for text-to-speech conversion.
    /// </summary>
    public void SetTtsCallback(Func<string, CancellationToken, Task<byte[]>> callback)
    {
        _ttsCallback = callback;
    }

    #endregion

    #region Private Methods

    private async Task ProcessCaptureWithVadAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var frame in _capture.ReadFramesAsync(ct))
            {
                bool shouldTransmit = _micMode switch
                {
                    "ptt" => _pttActive,
                    "vox" => _vad.Analyze(frame),
                    _ => false
                };

                // Check for voice activity state change
                if (shouldTransmit != _isVoiceActive)
                {
                    _isVoiceActive = shouldTransmit;
                    _logger.LogDebug("Voice activity changed: {Active}", _isVoiceActive);

                    // Fire event on thread pool to avoid blocking audio processing
                    var args = new VoiceActivityEventArgs(_isVoiceActive);
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            VoiceActivityChanged?.Invoke(this, args);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in VoiceActivityChanged handler");
                        }
                    });
                }

                if (shouldTransmit)
                {
                    await _captureOutputChannel.Writer.WriteAsync(frame, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing captured audio");
        }
        finally
        {
            // Ensure we signal voice stopped when capture ends
            if (_isVoiceActive)
            {
                _isVoiceActive = false;
                VoiceActivityChanged?.Invoke(this, new VoiceActivityEventArgs(false));
            }
        }
    }

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove shell-dangerous characters
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            text, @"[;|&`$(){}[\]<>]", "");

        // Remove SSML-like tags
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"<[^>]+>", "");

        // Limit length
        return sanitized.Length > 500 ? sanitized[..500] : sanitized;
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _vadCts?.Cancel();
        _player.Dispose();
        _capture.Dispose();
        _vadCts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
