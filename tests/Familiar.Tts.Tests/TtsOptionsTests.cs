using FluentAssertions;
using Xunit;

namespace Familiar.Tts.Tests;

/// <summary>
/// Unit tests for TtsOptions configuration.
/// </summary>
public class TtsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new TtsOptions();

        options.Engine.Should().Be("espeak");
        options.Voice.Should().Be("en");
        options.Rate.Should().Be(150);
        options.Pitch.Should().Be(0);
        options.Volume.Should().Be(100);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        TtsOptions.SectionName.Should().Be("Familiar:Tts");
    }

    [Theory]
    [InlineData("espeak")]
    [InlineData("festival")]
    [InlineData("pico")]
    public void Engine_AcceptsValidEngines(string engine)
    {
        var options = new TtsOptions() with { Engine = engine };
        options.Engine.Should().Be(engine);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-us")]
    [InlineData("en-gb")]
    [InlineData("es")]
    [InlineData("de")]
    [InlineData("fr")]
    public void Voice_AcceptsValidVoices(string voice)
    {
        var options = new TtsOptions() with { Voice = voice };
        options.Voice.Should().Be(voice);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(400)]
    public void Rate_AcceptsValidRates(int rate)
    {
        var options = new TtsOptions() with { Rate = rate };
        options.Rate.Should().Be(rate);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-50)]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Pitch_AcceptsValidRange(int pitch)
    {
        var options = new TtsOptions() with { Pitch = pitch };
        options.Pitch.Should().Be(pitch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    public void Volume_AcceptsValidRange(int volume)
    {
        var options = new TtsOptions() with { Volume = volume };
        options.Volume.Should().Be(volume);
    }

    [Fact]
    public void Record_WithExpression_CreatesNewInstance()
    {
        var original = new TtsOptions();
        var modified = original with { Voice = "es", Rate = 200 };

        original.Voice.Should().Be("en");
        original.Rate.Should().Be(150);
        modified.Voice.Should().Be("es");
        modified.Rate.Should().Be(200);
    }
}
