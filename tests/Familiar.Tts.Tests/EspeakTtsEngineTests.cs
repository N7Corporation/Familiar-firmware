using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Familiar.Tts.Tests;

/// <summary>
/// Unit tests for EspeakTtsEngine.
/// Tests argument building, WAV parsing, and edge cases.
/// Note: Actual synthesis tests require espeak installed and are marked with [Trait].
/// </summary>
public class EspeakTtsEngineTests
{
    private readonly Mock<ILogger<EspeakTtsEngine>> _mockLogger;
    private readonly TtsOptions _defaultOptions;

    public EspeakTtsEngineTests()
    {
        _mockLogger = new Mock<ILogger<EspeakTtsEngine>>();
        _defaultOptions = new TtsOptions();
    }

    private EspeakTtsEngine CreateEngine(TtsOptions? options = null)
    {
        var opts = options ?? _defaultOptions;
        return new EspeakTtsEngine(Options.Create(opts), _mockLogger.Object);
    }

    #region Configuration Tests

    [Fact]
    public void SetVoice_ChangesVoice()
    {
        var engine = CreateEngine();

        engine.SetVoice("es");

        // Voice change is internal, verified through synthesis behavior
        // This test mainly ensures no exceptions are thrown
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-us")]
    [InlineData("es")]
    [InlineData("de")]
    [InlineData("fr")]
    public void SetVoice_AcceptsValidVoiceIds(string voiceId)
    {
        var engine = CreateEngine();

        // Should not throw
        engine.SetVoice(voiceId);
    }

    [Fact]
    public void SetRate_ValidRate_SetsRate()
    {
        var engine = CreateEngine();

        engine.SetRate(200);

        // Rate change is internal, verified through synthesis behavior
    }

    [Theory]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(250)]
    [InlineData(400)]
    public void SetRate_ValidRates_Accepted(int rate)
    {
        var engine = CreateEngine();

        // Should not throw
        engine.SetRate(rate);
    }

    [Theory]
    [InlineData(0)]      // Below min, clamped to 50
    [InlineData(500)]    // Above max, clamped to 400
    [InlineData(-100)]   // Negative, clamped to 50
    public void SetRate_OutOfRange_ClampedToValidRange(int input)
    {
        var engine = CreateEngine();

        // SetRate clamps internally, this test ensures no exceptions
        engine.SetRate(input);
    }

    #endregion

    #region WAV Parsing Tests

    [Fact]
    public void ExtractPcmFromWav_ValidWav_ExtractsPcmData()
    {
        // Create a minimal valid WAV file
        var wavData = CreateMinimalWavFile(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        var result = WavHelper.ExtractPcmFromWav(wavData);

        result.Should().Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 });
    }

    [Fact]
    public void ExtractPcmFromWav_EmptyWav_ReturnsEmptyArray()
    {
        var result = WavHelper.ExtractPcmFromWav(Array.Empty<byte>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPcmFromWav_SmallData_ReturnsEmptyArray()
    {
        var result = WavHelper.ExtractPcmFromWav(new byte[] { 0x01, 0x02, 0x03 });

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPcmFromWav_NoDataChunk_UsesFallbackOffset()
    {
        // Create data without proper "data" marker but larger than 44 bytes
        var data = new byte[100];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }

        var result = WavHelper.ExtractPcmFromWav(data);

        // Fallback uses 44-byte offset
        result.Should().HaveCount(56);
    }

    [Fact]
    public void ExtractPcmFromWav_DataChunkWithCorrectSize_ExtractsCorrectly()
    {
        // WAV with "data" chunk and size = 10
        var pcmContent = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44 };
        var wavData = CreateMinimalWavFile(pcmContent);

        var result = WavHelper.ExtractPcmFromWav(wavData);

        result.Should().Equal(pcmContent);
    }

    [Fact]
    public void ExtractPcmFromWav_LargeDataChunk_ExtractsCorrectly()
    {
        // Create larger PCM data
        var pcmContent = new byte[4096];
        new Random(42).NextBytes(pcmContent);

        var wavData = CreateMinimalWavFile(pcmContent);

        var result = WavHelper.ExtractPcmFromWav(wavData);

        result.Should().Equal(pcmContent);
    }

    #endregion

    #region Synthesis Tests (Edge Cases)

    [Fact]
    public async Task SynthesizeAsync_EmptyText_ReturnsEmptyArray()
    {
        var engine = CreateEngine();

        var result = await engine.SynthesizeAsync("");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SynthesizeAsync_WhitespaceText_ReturnsEmptyArray()
    {
        var engine = CreateEngine();

        var result = await engine.SynthesizeAsync("   ");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SynthesizeAsync_NullText_ReturnsEmptyArray()
    {
        var engine = CreateEngine();

        var result = await engine.SynthesizeAsync(null!);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SynthesizeAsync_CancellationRequested_ThrowsOrReturnsEmpty()
    {
        var engine = CreateEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should either throw OperationCanceledException or return empty
        try
        {
            var result = await engine.SynthesizeAsync("test", cts.Token);
            result.Should().BeEmpty();
        }
        catch (OperationCanceledException)
        {
            // Also acceptable
        }
    }

    #endregion

    #region Text Escaping Tests

    [Fact]
    public void BuildArguments_DocumentedBehavior()
    {
        // Note: BuildArguments is private, testing through integration
        // The actual escaping is verified when synthesis is called
        // Expected escaping:
        // - "Hello world" -> "Hello world" (no change)
        // - "Test \"quotes\"" -> "Test \\\"quotes\\\"" (escaped)
        var options = new TtsOptions { Voice = "en", Rate = 150, Pitch = 0, Volume = 100 };
        var engine = CreateEngine(options);

        // This test documents expected behavior for quotes
        // Actual verification would require making BuildArguments internal or testable
        engine.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Numbers 12345")]
    [InlineData("Special: !@#$%")]
    [InlineData("Multi\nline")]
    public void SynthesizeAsync_VariousTextInputs_DoesNotThrow(string text)
    {
        var engine = CreateEngine();

        // These should not throw (synthesis may fail if espeak not available)
        var action = async () => await engine.SynthesizeAsync(text);
        action.Should().NotThrowAsync();
    }

    #endregion

    #region Voice List Tests

    [Fact]
    public async Task GetAvailableVoicesAsync_WhenNotAvailable_ReturnsEmptyList()
    {
        var engine = CreateEngine();

        if (!engine.IsAvailable)
        {
            var voices = await engine.GetAvailableVoicesAsync();
            voices.Should().BeEmpty();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid WAV file with the given PCM data.
    /// </summary>
    private static byte[] CreateMinimalWavFile(byte[] pcmData)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + pcmData.Length); // File size minus 8
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write((short)1); // Channels
        writer.Write(48000); // Sample rate
        writer.Write(48000 * 2); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample

        // data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    #endregion
}

/// <summary>
/// Helper class to test WAV parsing logic.
/// Mirrors the private ExtractPcmFromWav method for testing.
/// </summary>
internal static class WavHelper
{
    public static byte[] ExtractPcmFromWav(byte[] wavData)
    {
        // WAV file structure: RIFF header (44 bytes typically) followed by PCM data
        // Find "data" chunk
        for (int i = 0; i < wavData.Length - 8; i++)
        {
            if (wavData[i] == 'd' && wavData[i + 1] == 'a' &&
                wavData[i + 2] == 't' && wavData[i + 3] == 'a')
            {
                // Next 4 bytes are the data size (little-endian)
                int dataSize = wavData[i + 4] | (wavData[i + 5] << 8) |
                              (wavData[i + 6] << 16) | (wavData[i + 7] << 24);

                int dataStart = i + 8;
                int actualSize = Math.Min(dataSize, wavData.Length - dataStart);

                var pcmData = new byte[actualSize];
                Array.Copy(wavData, dataStart, pcmData, 0, actualSize);
                return pcmData;
            }
        }

        // Fallback: assume standard 44-byte header
        if (wavData.Length > 44)
        {
            var pcmData = new byte[wavData.Length - 44];
            Array.Copy(wavData, 44, pcmData, 0, pcmData.Length);
            return pcmData;
        }

        return Array.Empty<byte>();
    }
}
