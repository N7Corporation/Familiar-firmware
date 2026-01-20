using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Familiar.Audio;

namespace Familiar.Benchmarks;

/// <summary>
/// Performance benchmarks for VoiceActivityDetector.
/// Critical for real-time audio processing - must process faster than real-time.
///
/// Run with: dotnet run -c Release -- --filter *VoiceActivityDetector*
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class VoiceActivityDetectorBenchmarks
{
    private VoiceActivityDetector _vad = null!;
    private byte[] _silentFrame = null!;
    private byte[] _loudFrame = null!;
    private byte[] _sineWaveFrame = null!;
    private byte[] _noiseFrame = null!;
    private byte[] _largeFrame = null!;

    /// <summary>
    /// Frame size: 20ms at 48kHz = 960 samples = 1920 bytes (16-bit PCM).
    /// This is a typical frame size for real-time audio processing.
    /// </summary>
    private const int TypicalFrameSamples = 960;

    /// <summary>
    /// Large frame: 1 second at 48kHz = 48000 samples = 96000 bytes.
    /// Tests worst-case scenario.
    /// </summary>
    private const int LargeFrameSamples = 48000;

    [GlobalSetup]
    public void Setup()
    {
        _vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: 25);

        _silentFrame = CreateSilentFrame(TypicalFrameSamples);
        _loudFrame = CreateConstantFrame(TypicalFrameSamples, 0.5f);
        _sineWaveFrame = CreateSineWaveFrame(TypicalFrameSamples, 440f, 0.5f);
        _noiseFrame = CreateNoiseFrame(TypicalFrameSamples, 0.3f);
        _largeFrame = CreateConstantFrame(LargeFrameSamples, 0.5f);
    }

    /// <summary>
    /// Baseline: processing silent audio (should be fast path).
    /// Target: &lt; 1 microsecond per frame.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool AnalyzeSilentFrame()
    {
        return _vad.Analyze(_silentFrame);
    }

    /// <summary>
    /// Processing loud audio that triggers detection.
    /// Target: &lt; 5 microseconds per frame.
    /// </summary>
    [Benchmark]
    public bool AnalyzeLoudFrame()
    {
        return _vad.Analyze(_loudFrame);
    }

    /// <summary>
    /// Processing realistic sine wave audio.
    /// Target: &lt; 5 microseconds per frame.
    /// </summary>
    [Benchmark]
    public bool AnalyzeSineWaveFrame()
    {
        return _vad.Analyze(_sineWaveFrame);
    }

    /// <summary>
    /// Processing random noise audio.
    /// Target: &lt; 5 microseconds per frame.
    /// </summary>
    [Benchmark]
    public bool AnalyzeNoiseFrame()
    {
        return _vad.Analyze(_noiseFrame);
    }

    /// <summary>
    /// Worst case: processing 1 second of audio at once.
    /// Should still be fast enough for batch processing.
    /// Target: &lt; 100 microseconds.
    /// </summary>
    [Benchmark]
    public bool AnalyzeLargeFrame()
    {
        return _vad.Analyze(_largeFrame);
    }

    /// <summary>
    /// Simulates 1 second of real-time processing (50 frames at 20ms each).
    /// Must complete in &lt;&lt; 1 second for real-time viability.
    /// </summary>
    [Benchmark]
    public int AnalyzeOneSecondOfAudio()
    {
        int activeCount = 0;
        for (int i = 0; i < 50; i++)
        {
            if (_vad.Analyze(_loudFrame))
                activeCount++;
        }
        return activeCount;
    }

    /// <summary>
    /// Simulates 10 seconds of real-time processing.
    /// Tests sustained performance without degradation.
    /// </summary>
    [Benchmark]
    public int AnalyzeTenSecondsOfAudio()
    {
        int activeCount = 0;
        for (int i = 0; i < 500; i++)
        {
            if (_vad.Analyze(i % 2 == 0 ? _loudFrame : _silentFrame))
                activeCount++;
        }
        return activeCount;
    }

    #region Helper Methods

    private static byte[] CreateSilentFrame(int samples)
    {
        return new byte[samples * 2];
    }

    private static byte[] CreateConstantFrame(int samples, float amplitude)
    {
        var buffer = new byte[samples * 2];
        var value = (short)(amplitude * 32767);

        for (int i = 0; i < samples; i++)
        {
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return buffer;
    }

    private static byte[] CreateSineWaveFrame(int samples, float frequency, float amplitude, int sampleRate = 48000)
    {
        var buffer = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            var t = (float)i / sampleRate;
            var sample = (short)(amplitude * 32767 * MathF.Sin(2 * MathF.PI * frequency * t));
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }

    private static byte[] CreateNoiseFrame(int samples, float amplitude)
    {
        var buffer = new byte[samples * 2];
        var random = new Random(42);

        for (int i = 0; i < samples; i++)
        {
            var sample = (short)(amplitude * 32767 * (random.NextSingle() * 2 - 1));
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }

    #endregion
}
