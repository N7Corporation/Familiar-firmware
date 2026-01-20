using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading.Channels;
using Xunit;

namespace Familiar.Audio.Tests;

/// <summary>
/// Unit tests for AudioManager.
/// Uses mocks for external dependencies (player, capture).
/// </summary>
public class AudioManagerTests : IDisposable
{
    private readonly Mock<IAudioPlayer> _mockPlayer;
    private readonly Mock<IAudioCapture> _mockCapture;
    private readonly Mock<ILogger<AudioManager>> _mockLogger;
    private readonly AudioOptions _options;
    private readonly AudioManager _audioManager;

    public AudioManagerTests()
    {
        _mockPlayer = new Mock<IAudioPlayer>();
        _mockCapture = new Mock<IAudioCapture>();
        _mockLogger = new Mock<ILogger<AudioManager>>();
        _options = new AudioOptions
        {
            Volume = 0.8f,
            MicMode = "vox",
            VoxThreshold = 0.02f,
            VoxHoldMs = 500
        };

        var optionsWrapper = Options.Create(_options);

        _audioManager = new AudioManager(
            _mockPlayer.Object,
            _mockCapture.Object,
            optionsWrapper,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _audioManager.Dispose();
    }

    #region Volume Tests

    [Fact]
    public void Volume_InitialValue_MatchesOptions()
    {
        _audioManager.Volume.Should().BeApproximately(0.8f, 0.001f);
    }

    [Fact]
    public void SetVolume_ValidValue_UpdatesVolumeAndPlayer()
    {
        _audioManager.SetVolume(0.5f);

        _audioManager.Volume.Should().BeApproximately(0.5f, 0.001f);
        _mockPlayer.Verify(p => p.SetVolume(0.5f), Times.Once);
    }

    [Fact]
    public void SetVolume_AboveOne_ClampsToOne()
    {
        _audioManager.SetVolume(1.5f);

        _audioManager.Volume.Should().BeApproximately(1.0f, 0.001f);
        _mockPlayer.Verify(p => p.SetVolume(1.0f), Times.Once);
    }

    [Fact]
    public void SetVolume_BelowZero_ClampsToZero()
    {
        _audioManager.SetVolume(-0.5f);

        _audioManager.Volume.Should().BeApproximately(0.0f, 0.001f);
        _mockPlayer.Verify(p => p.SetVolume(0.0f), Times.Once);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    [InlineData(1.0f)]
    public void SetVolume_ValidRange_SetsCorrectly(float volume)
    {
        _audioManager.SetVolume(volume);

        _audioManager.Volume.Should().BeApproximately(volume, 0.001f);
    }

    #endregion

    #region Mute Tests

    [Fact]
    public void IsMuted_InitialValue_IsFalse()
    {
        _audioManager.IsMuted.Should().BeFalse();
    }

    [Fact]
    public void IsMuted_SetTrue_MutesAudio()
    {
        _audioManager.IsMuted = true;

        _audioManager.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void IsMuted_Toggle_Works()
    {
        _audioManager.IsMuted = true;
        _audioManager.IsMuted.Should().BeTrue();

        _audioManager.IsMuted = false;
        _audioManager.IsMuted.Should().BeFalse();
    }

    [Fact]
    public async Task PlayStreamAsync_WhenMuted_DoesNotPlayAudio()
    {
        _audioManager.IsMuted = true;
        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        await _audioManager.PlayStreamAsync(audioData);

        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlayStreamAsync_WhenNotMuted_PlaysAudio()
    {
        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        await _audioManager.PlayStreamAsync(audioData);

        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Mic Mode Tests

    [Fact]
    public void MicMode_InitialValue_MatchesOptions()
    {
        _audioManager.MicMode.Should().Be("vox");
    }

    [Theory]
    [InlineData("vox", "vox")]
    [InlineData("VOX", "vox")]
    [InlineData("Vox", "vox")]
    [InlineData("ptt", "ptt")]
    [InlineData("PTT", "ptt")]
    [InlineData("Ptt", "ptt")]
    public void MicMode_SetValue_NormalizesToLowercase(string input, string expected)
    {
        _audioManager.MicMode = input;

        _audioManager.MicMode.Should().Be(expected);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("auto")]
    public void MicMode_InvalidValue_DefaultsToVox(string input)
    {
        _audioManager.MicMode = input;

        _audioManager.MicMode.Should().Be("vox");
    }

    #endregion

    #region PTT Tests

    [Fact]
    public void PttActive_InitialValue_IsFalse()
    {
        _audioManager.PttActive.Should().BeFalse();
    }

    [Fact]
    public void PttActive_SetTrue_ActivatesPtt()
    {
        _audioManager.PttActive = true;

        _audioManager.PttActive.Should().BeTrue();
    }

    [Fact]
    public void PttActive_Toggle_Works()
    {
        _audioManager.PttActive = true;
        _audioManager.PttActive.Should().BeTrue();

        _audioManager.PttActive = false;
        _audioManager.PttActive.Should().BeFalse();
    }

    #endregion

    #region Capture Tests

    [Fact]
    public void IsCapturing_InitialValue_IsFalse()
    {
        _mockCapture.Setup(c => c.IsCapturing).Returns(false);

        _audioManager.IsCapturing.Should().BeFalse();
    }

    [Fact]
    public async Task StartCaptureAsync_StartsCapture()
    {
        _mockCapture.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockCapture.Setup(c => c.ReadFramesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<byte[]>());

        await _audioManager.StartCaptureAsync();

        _mockCapture.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopCaptureAsync_StopsCapture()
    {
        _mockCapture.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockCapture.Setup(c => c.StopAsync())
            .Returns(Task.CompletedTask);
        _mockCapture.Setup(c => c.ReadFramesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<byte[]>());

        await _audioManager.StartCaptureAsync();
        await _audioManager.StopCaptureAsync();

        _mockCapture.Verify(c => c.StopAsync(), Times.Once);
    }

    #endregion

    #region TTS Callback Tests

    [Fact]
    public async Task PlayTtsAsync_WithoutCallback_DoesNotThrow()
    {
        // No TTS callback configured
        await _audioManager.PlayTtsAsync("Hello");

        // Should not throw, just log warning
        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlayTtsAsync_WithCallback_PlaysAudio()
    {
        var ttsAudio = new byte[] { 0x01, 0x02, 0x03 };
        _audioManager.SetTtsCallback((text, ct) => Task.FromResult(ttsAudio));

        await _audioManager.PlayTtsAsync("Hello");

        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlayTtsAsync_WhenMuted_DoesNotPlay()
    {
        var ttsAudio = new byte[] { 0x01, 0x02, 0x03 };
        _audioManager.SetTtsCallback((text, ct) => Task.FromResult(ttsAudio));
        _audioManager.IsMuted = true;

        await _audioManager.PlayTtsAsync("Hello");

        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task PlayTtsAsync_EmptyText_DoesNotPlay(string? text)
    {
        var ttsAudio = new byte[] { 0x01, 0x02, 0x03 };
        _audioManager.SetTtsCallback((t, ct) => Task.FromResult(ttsAudio));

        await _audioManager.PlayTtsAsync(text!);

        _mockPlayer.Verify(p => p.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Hello; rm -rf /")]
    [InlineData("Test | cat /etc/passwd")]
    [InlineData("$(whoami)")]
    [InlineData("Test `id`")]
    public async Task PlayTtsAsync_SanitizesDangerousCharacters(string text)
    {
        string? receivedText = null;
        _audioManager.SetTtsCallback((t, ct) =>
        {
            receivedText = t;
            return Task.FromResult(new byte[] { 0x01 });
        });

        await _audioManager.PlayTtsAsync(text);

        receivedText.Should().NotContain(";");
        receivedText.Should().NotContain("|");
        receivedText.Should().NotContain("$");
        receivedText.Should().NotContain("`");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesPlayer()
    {
        _audioManager.Dispose();

        _mockPlayer.Verify(p => p.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_DisposesCapture()
    {
        _audioManager.Dispose();

        _mockCapture.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
    {
        _audioManager.Dispose();
        _audioManager.Dispose();
        _audioManager.Dispose();

        _mockPlayer.Verify(p => p.Dispose(), Times.Once);
        _mockCapture.Verify(c => c.Dispose(), Times.Once);
    }

    #endregion
}

/// <summary>
/// Helper class to create empty async enumerables for mocking.
/// </summary>
internal static class AsyncEnumerable
{
    public static IAsyncEnumerable<T> Empty<T>()
    {
        return new EmptyAsyncEnumerable<T>();
    }

    private class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new EmptyAsyncEnumerator<T>();
        }
    }

    private class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        public T Current => default!;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
    }
}
