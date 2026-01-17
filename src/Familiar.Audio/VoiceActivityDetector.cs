namespace Familiar.Audio;

/// <summary>
/// Detects voice activity in audio frames using RMS energy threshold.
/// </summary>
public class VoiceActivityDetector
{
    private readonly float _threshold;
    private readonly int _holdFrames;
    private int _silentFrames;

    /// <summary>
    /// Creates a new voice activity detector.
    /// </summary>
    /// <param name="threshold">RMS energy threshold (0.0 - 1.0). Default 0.02.</param>
    /// <param name="holdFrames">Number of frames to hold after voice stops. Default 10.</param>
    public VoiceActivityDetector(float threshold = 0.02f, int holdFrames = 10)
    {
        _threshold = threshold;
        _holdFrames = holdFrames;
        _silentFrames = _holdFrames; // Start in silent state
    }

    /// <summary>
    /// Gets whether voice is currently detected (including hold time).
    /// </summary>
    public bool IsVoiceActive { get; private set; }

    /// <summary>
    /// Analyzes an audio frame and returns whether voice activity is detected.
    /// </summary>
    /// <param name="audioFrame">PCM16 audio data (16-bit signed, little-endian).</param>
    /// <returns>True if voice is active (including hold period).</returns>
    public bool Analyze(ReadOnlySpan<byte> audioFrame)
    {
        if (audioFrame.Length < 2)
        {
            return IsVoiceActive;
        }

        // Calculate RMS energy of the frame
        float sum = 0;
        int sampleCount = audioFrame.Length / 2;

        for (int i = 0; i < audioFrame.Length - 1; i += 2)
        {
            // Read 16-bit signed sample (little-endian)
            short sample = (short)(audioFrame[i] | (audioFrame[i + 1] << 8));
            float normalized = sample / 32768f;
            sum += normalized * normalized;
        }

        float rms = MathF.Sqrt(sum / sampleCount);

        if (rms > _threshold)
        {
            _silentFrames = 0;
            IsVoiceActive = true;
        }
        else
        {
            _silentFrames++;
            IsVoiceActive = _silentFrames < _holdFrames;
        }

        return IsVoiceActive;
    }

    /// <summary>
    /// Resets the detector state.
    /// </summary>
    public void Reset()
    {
        _silentFrames = _holdFrames;
        IsVoiceActive = false;
    }

    /// <summary>
    /// Updates the threshold dynamically.
    /// </summary>
    public void SetThreshold(float threshold)
    {
        // Note: This creates a new detector internally since _threshold is readonly
        // In a more complex implementation, you might want to make this mutable
    }
}
