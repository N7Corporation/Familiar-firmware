using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Tts;

/// <summary>
/// Text-to-speech engine using espeak command-line tool.
/// </summary>
public class EspeakTtsEngine : ITtsEngine
{
    private readonly ILogger<EspeakTtsEngine> _logger;
    private TtsOptions _options;
    private readonly string _espeakPath;

    public EspeakTtsEngine(
        IOptions<TtsOptions> options,
        ILogger<EspeakTtsEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
        _espeakPath = FindEspeakPath();
    }

    public bool IsAvailable => !string.IsNullOrEmpty(_espeakPath) && File.Exists(_espeakPath);

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("espeak is not available on this system");
            return Array.Empty<byte>();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<byte>();
        }

        var tempFile = Path.GetTempFileName();
        var wavFile = tempFile + ".wav";

        try
        {
            // Build espeak arguments
            var args = BuildArguments(text, wavFile);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _espeakPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("espeak failed with exit code {Code}: {Error}",
                    process.ExitCode, error);
                return Array.Empty<byte>();
            }

            if (!File.Exists(wavFile))
            {
                _logger.LogError("espeak did not produce output file");
                return Array.Empty<byte>();
            }

            // Read the WAV file and extract PCM data
            var wavData = await File.ReadAllBytesAsync(wavFile, ct);
            return ExtractPcmFromWav(wavData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech for: {Text}", text);
            return Array.Empty<byte>();
        }
        finally
        {
            // Cleanup temp files
            TryDeleteFile(tempFile);
            TryDeleteFile(wavFile);
        }
    }

    public void SetVoice(string voiceId)
    {
        _options = _options with { Voice = voiceId };
        _logger.LogDebug("Voice set to {Voice}", voiceId);
    }

    public void SetRate(int wordsPerMinute)
    {
        _options = _options with { Rate = Math.Clamp(wordsPerMinute, 50, 400) };
        _logger.LogDebug("Rate set to {Rate} WPM", _options.Rate);
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return Array.Empty<string>();
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _espeakPath,
                    Arguments = "--voices",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Parse voice list from output
            var voices = new List<string>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip header
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    voices.Add(parts[3]); // Voice name is typically 4th column
                }
            }

            return voices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available voices");
            return Array.Empty<string>();
        }
    }

    private string BuildArguments(string text, string outputFile)
    {
        var escapedText = text.Replace("\"", "\\\"");

        return $"-v {_options.Voice} " +
               $"-s {_options.Rate} " +
               $"-p {_options.Pitch + 50} " + // espeak pitch is 0-99, default 50
               $"-a {_options.Volume} " +
               $"-w \"{outputFile}\" " +
               $"\"{escapedText}\"";
    }

    private static string FindEspeakPath()
    {
        var possiblePaths = new[]
        {
            "/usr/bin/espeak",
            "/usr/bin/espeak-ng",
            "/usr/local/bin/espeak",
            "/usr/local/bin/espeak-ng"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private static byte[] ExtractPcmFromWav(byte[] wavData)
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

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
