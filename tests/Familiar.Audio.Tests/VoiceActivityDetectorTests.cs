using FluentAssertions;
using Xunit;

namespace Familiar.Audio.Tests;

/// <summary>
/// Unit tests for VoiceActivityDetector.
/// Tests voice detection, hold time, and edge cases.
/// </summary>
public class VoiceActivityDetectorTests
{
    #region Helper Methods

    /// <summary>
    /// Creates a PCM16 audio frame with a specific amplitude.
    /// </summary>
    private static byte[] CreateAudioFrame(int sampleCount, float amplitude)
    {
        var buffer = new byte[sampleCount * 2]; // 16-bit = 2 bytes per sample
        var value = (short)(amplitude * 32767);

        for (int i = 0; i < sampleCount; i++)
        {
            // Little-endian 16-bit samples
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return buffer;
    }

    /// <summary>
    /// Creates a silent audio frame (all zeros).
    /// </summary>
    private static byte[] CreateSilentFrame(int sampleCount)
    {
        return new byte[sampleCount * 2];
    }

    /// <summary>
    /// Creates a sine wave audio frame for more realistic testing.
    /// </summary>
    private static byte[] CreateSineWaveFrame(int sampleCount, float frequency, float amplitude, int sampleRate = 48000)
    {
        var buffer = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            var t = (float)i / sampleRate;
            var sample = (short)(amplitude * 32767 * MathF.Sin(2 * MathF.PI * frequency * t));
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }

    /// <summary>
    /// Creates random noise audio frame.
    /// </summary>
    private static byte[] CreateNoiseFrame(int sampleCount, float amplitude)
    {
        var buffer = new byte[sampleCount * 2];
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < sampleCount; i++)
        {
            var sample = (short)(amplitude * 32767 * (random.NextSingle() * 2 - 1));
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultValues_SetsCorrectDefaults()
    {
        var vad = new VoiceActivityDetector();

        vad.IsVoiceActive.Should().BeFalse("VAD should start in silent state");
    }

    [Fact]
    public void Constructor_WithCustomThreshold_UsesCustomThreshold()
    {
        var vad = new VoiceActivityDetector(threshold: 0.1f);

        // A moderate signal should not trigger with high threshold
        var moderateSignal = CreateAudioFrame(480, 0.05f);
        vad.Analyze(moderateSignal);

        vad.IsVoiceActive.Should().BeFalse("Signal below threshold should not activate");
    }

    [Fact]
    public void Constructor_WithCustomHoldFrames_UsesCustomHoldFrames()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: 3);

        // Activate with loud signal
        var loudSignal = CreateAudioFrame(480, 0.5f);
        vad.Analyze(loudSignal);
        vad.IsVoiceActive.Should().BeTrue();

        // Send silent frames - should stay active for 3 frames (hold time)
        var silentFrame = CreateSilentFrame(480);
        vad.Analyze(silentFrame).Should().BeTrue("Frame 1 of hold");
        vad.Analyze(silentFrame).Should().BeTrue("Frame 2 of hold");
        vad.Analyze(silentFrame).Should().BeFalse("Frame 3 should deactivate");
    }

    #endregion

    #region Voice Detection Tests

    [Fact]
    public void Analyze_SilentAudio_ReturnsInactive()
    {
        var vad = new VoiceActivityDetector();
        var silentFrame = CreateSilentFrame(480);

        var result = vad.Analyze(silentFrame);

        result.Should().BeFalse("Silent audio should not trigger voice detection");
        vad.IsVoiceActive.Should().BeFalse();
    }

    [Fact]
    public void Analyze_LoudAudio_ReturnsActive()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f);
        var loudFrame = CreateAudioFrame(480, 0.5f);

        var result = vad.Analyze(loudFrame);

        result.Should().BeTrue("Loud audio should trigger voice detection");
        vad.IsVoiceActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.01f, false)]  // Below threshold (0.01 < 0.015)
    [InlineData(0.02f, true)]   // Above threshold (0.02 > 0.015)
    [InlineData(0.03f, true)]   // Well above threshold
    [InlineData(0.005f, false)] // Well below threshold
    public void Analyze_VariousAmplitudes_DetectsCorrectly(float amplitude, bool shouldDetect)
    {
        var vad = new VoiceActivityDetector(threshold: 0.015f); // Threshold is 0.015
        var frame = CreateAudioFrame(480, amplitude);

        var result = vad.Analyze(frame);

        if (shouldDetect)
        {
            result.Should().BeTrue($"Amplitude {amplitude} should be detected");
        }
        else
        {
            result.Should().BeFalse($"Amplitude {amplitude} should not be detected");
        }
    }

    [Fact]
    public void Analyze_SineWave_DetectsVoice()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f);

        // 440Hz sine wave at 50% amplitude (typical voice frequency)
        var sineWave = CreateSineWaveFrame(480, 440f, 0.5f);

        var result = vad.Analyze(sineWave);

        result.Should().BeTrue("Sine wave should trigger voice detection");
    }

    [Fact]
    public void Analyze_RandomNoise_DetectsBasedOnAmplitude()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f);

        var loudNoise = CreateNoiseFrame(480, 0.3f);
        vad.Analyze(loudNoise).Should().BeTrue("Loud noise should trigger");

        vad.Reset();

        var quietNoise = CreateNoiseFrame(480, 0.005f);
        vad.Analyze(quietNoise).Should().BeFalse("Quiet noise should not trigger");
    }

    #endregion

    #region Hold Time Tests

    [Fact]
    public void Analyze_AfterVoiceStops_HoldsForConfiguredFrames()
    {
        const int holdFrames = 5;
        var vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: holdFrames);

        // Activate
        var loudFrame = CreateAudioFrame(480, 0.5f);
        vad.Analyze(loudFrame);
        vad.IsVoiceActive.Should().BeTrue();

        // Verify hold period
        var silentFrame = CreateSilentFrame(480);
        for (int i = 0; i < holdFrames - 1; i++)
        {
            vad.Analyze(silentFrame).Should().BeTrue($"Should hold at frame {i + 1} of {holdFrames}");
        }

        // Should deactivate after hold frames
        vad.Analyze(silentFrame).Should().BeFalse("Should deactivate after hold period");
    }

    [Fact]
    public void Analyze_VoiceResumesWithinHoldTime_StaysActive()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: 10);

        var loudFrame = CreateAudioFrame(480, 0.5f);
        var silentFrame = CreateSilentFrame(480);

        // Activate
        vad.Analyze(loudFrame);

        // Go silent for a few frames (within hold time)
        for (int i = 0; i < 5; i++)
        {
            vad.Analyze(silentFrame);
        }
        vad.IsVoiceActive.Should().BeTrue("Should still be in hold period");

        // Resume voice
        vad.Analyze(loudFrame);

        // Hold counter should be reset
        vad.IsVoiceActive.Should().BeTrue();

        // Verify hold time is reset (should need full hold frames again)
        for (int i = 0; i < 9; i++)
        {
            vad.Analyze(silentFrame).Should().BeTrue($"Hold should be reset, frame {i + 1}");
        }
        vad.Analyze(silentFrame).Should().BeFalse("Should deactivate after full new hold period");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Analyze_EmptyFrame_ReturnsPreviousState()
    {
        var vad = new VoiceActivityDetector();

        var result = vad.Analyze(Array.Empty<byte>());

        result.Should().BeFalse("Empty frame should return previous state (initially false)");
    }

    [Fact]
    public void Analyze_SingleByte_ReturnsPreviousState()
    {
        var vad = new VoiceActivityDetector();

        var result = vad.Analyze(new byte[] { 0x00 });

        result.Should().BeFalse("Single byte frame should return previous state");
    }

    [Fact]
    public void Analyze_OddNumberOfBytes_HandlesGracefully()
    {
        var vad = new VoiceActivityDetector();

        // 3 bytes = 1.5 samples, should handle gracefully
        var oddFrame = new byte[] { 0xFF, 0x7F, 0x00 };
        var result = vad.Analyze(oddFrame);

        // Should not throw and should process the complete sample
        result.Should().BeTrue("Should process the one complete sample (max positive)");
    }

    [Fact]
    public void Analyze_MaxPositiveAmplitude_Detects()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f);

        // Max positive 16-bit value = 32767 = 0x7FFF (little-endian: 0xFF 0x7F)
        var maxFrame = new byte[] { 0xFF, 0x7F, 0xFF, 0x7F }; // Two max samples

        var result = vad.Analyze(maxFrame);

        result.Should().BeTrue("Maximum amplitude should trigger detection");
    }

    [Fact]
    public void Analyze_MaxNegativeAmplitude_Detects()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f);

        // Max negative 16-bit value = -32768 = 0x8000 (little-endian: 0x00 0x80)
        var minFrame = new byte[] { 0x00, 0x80, 0x00, 0x80 }; // Two min samples

        var result = vad.Analyze(minFrame);

        result.Should().BeTrue("Maximum negative amplitude should trigger detection");
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_AfterActivation_DeactivatesAndResets()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: 10);

        // Activate
        var loudFrame = CreateAudioFrame(480, 0.5f);
        vad.Analyze(loudFrame);
        vad.IsVoiceActive.Should().BeTrue();

        // Reset
        vad.Reset();

        vad.IsVoiceActive.Should().BeFalse("Reset should deactivate");

        // Next silent frame should not be in hold period
        var silentFrame = CreateSilentFrame(480);
        vad.Analyze(silentFrame).Should().BeFalse("Should not be active after reset");
    }

    [Fact]
    public void Reset_CalledMultipleTimes_IsIdempotent()
    {
        var vad = new VoiceActivityDetector();

        // Activate
        var loudFrame = CreateAudioFrame(480, 0.5f);
        vad.Analyze(loudFrame);

        // Reset multiple times
        vad.Reset();
        vad.Reset();
        vad.Reset();

        vad.IsVoiceActive.Should().BeFalse();
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public void Analyze_SequentialCalls_MaintainsCorrectState()
    {
        var vad = new VoiceActivityDetector(threshold: 0.02f, holdFrames: 5);

        var loudFrame = CreateAudioFrame(480, 0.5f);
        var silentFrame = CreateSilentFrame(480);

        // Simulate a conversation with pauses
        var states = new List<bool>();

        // Voice on
        for (int i = 0; i < 10; i++)
        {
            states.Add(vad.Analyze(loudFrame));
        }

        // Pause (should hold then deactivate)
        for (int i = 0; i < 10; i++)
        {
            states.Add(vad.Analyze(silentFrame));
        }

        // Voice on again
        for (int i = 0; i < 5; i++)
        {
            states.Add(vad.Analyze(loudFrame));
        }

        // Verify pattern
        states.Take(10).Should().AllBeEquivalentTo(true, "Voice active");
        states.Skip(10).Take(4).Should().AllBeEquivalentTo(true, "Hold period");
        states.Skip(14).Take(6).Should().AllBeEquivalentTo(false, "Inactive");
        states.Skip(20).Should().AllBeEquivalentTo(true, "Voice active again");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Analyze_LargeFrame_CompletesQuickly()
    {
        var vad = new VoiceActivityDetector();

        // 48000 samples = 1 second of audio at 48kHz
        var largeFrame = CreateAudioFrame(48000, 0.3f);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            vad.Analyze(largeFrame);
        }

        stopwatch.Stop();

        // Should process 100 seconds worth of audio in under 100ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "VAD should process audio faster than real-time");
    }

    [Fact]
    public void Analyze_TypicalFrameSize_HasLowLatency()
    {
        var vad = new VoiceActivityDetector();

        // 960 samples = 20ms at 48kHz (typical frame size)
        var typicalFrame = CreateAudioFrame(960, 0.3f);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
        {
            vad.Analyze(typicalFrame);
        }

        stopwatch.Stop();

        // 1000 frames at 20ms each = 20 seconds of audio
        // Should process in well under 1 second
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "VAD should have minimal latency for real-time processing");
    }

    #endregion
}
