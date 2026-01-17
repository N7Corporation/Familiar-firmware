namespace Familiar.Audio;

/// <summary>
/// Configuration options for audio playback and capture.
/// </summary>
public class AudioOptions
{
    public const string SectionName = "Familiar:Audio";

    /// <summary>
    /// ALSA output device name for playback.
    /// </summary>
    public string OutputDevice { get; set; } = "default";

    /// <summary>
    /// ALSA input device name for microphone capture.
    /// </summary>
    public string InputDevice { get; set; } = "default";

    /// <summary>
    /// Audio sample rate in Hz.
    /// </summary>
    public int SampleRate { get; set; } = 48000;

    /// <summary>
    /// Buffer size in bytes for audio processing.
    /// </summary>
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    /// Output volume level (0.0 - 1.0).
    /// </summary>
    public float Volume { get; set; } = 0.8f;

    /// <summary>
    /// Microphone mode: "vox" for voice-activated, "ptt" for push-to-talk.
    /// </summary>
    public string MicMode { get; set; } = "vox";

    /// <summary>
    /// Voice activity detection threshold (0.0 - 1.0).
    /// </summary>
    public float VoxThreshold { get; set; } = 0.02f;

    /// <summary>
    /// Time in milliseconds to hold transmission after voice stops.
    /// </summary>
    public int VoxHoldMs { get; set; } = 500;
}
