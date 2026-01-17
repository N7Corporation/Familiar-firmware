using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Familiar.Meshtastic;

/// <summary>
/// Background service that manages Meshtastic connection and message handling.
/// </summary>
public class MeshtasticService : BackgroundService
{
    private readonly IMeshtasticClient _client;
    private readonly MeshtasticOptions _options;
    private readonly ILogger<MeshtasticService> _logger;

    /// <summary>
    /// Event raised when a text message is received that should be spoken.
    /// </summary>
    public event Func<string, string, Task>? TextMessageReceived;

    /// <summary>
    /// Event raised when a command is received.
    /// </summary>
    public event Func<string, string, Task>? CommandReceived;

    public MeshtasticService(
        IMeshtasticClient client,
        IOptions<MeshtasticOptions> options,
        ILogger<MeshtasticService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public IMeshtasticClient Client => _client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Meshtastic service is disabled");
            return;
        }

        _client.MessageReceived += OnMessageReceived;
        _client.ConnectionStateChanged += OnConnectionStateChanged;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogInformation("Connecting to Meshtastic device...");
                    await _client.ConnectAsync(stoppingToken);
                }

                // Wait and check connection periodically
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Meshtastic connection error, retrying in {Delay}s",
                    _options.ReconnectDelaySeconds);

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.ReconnectDelaySeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _client.MessageReceived -= OnMessageReceived;
        _client.ConnectionStateChanged -= OnConnectionStateChanged;

        try
        {
            await _client.DisconnectAsync();
        }
        catch
        {
            // Ignore disconnect errors during shutdown
        }
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var text = e.Text.Trim();
        var fromNode = e.FromNode;

        _logger.LogDebug("Processing message from {Node}: {Text}", fromNode, text);

        try
        {
            if (text.StartsWith('!'))
            {
                // Command message
                await HandleCommand(text, fromNode);
            }
            else
            {
                // Text message - should be spoken via TTS
                await HandleTextMessage(text, fromNode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from {Node}", fromNode);
        }
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        _logger.LogInformation("Meshtastic connection state: {State}",
            connected ? "Connected" : "Disconnected");
    }

    private async Task HandleCommand(string command, string fromNode)
    {
        _logger.LogInformation("Command from {Node}: {Command}", fromNode, command);

        var parts = command.Split(' ', 2, StringSplitOptions.TrimEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        if (CommandReceived != null)
        {
            await CommandReceived.Invoke(command, fromNode);
        }

        // Built-in command responses
        switch (cmd)
        {
            case "!status":
                await _client.SendMessageAsync($"Familiar online. Nodes: {_client.KnownNodes.Count}", fromNode);
                break;

            case "!ping":
                await _client.SendMessageAsync("pong", fromNode);
                break;
        }
    }

    private async Task HandleTextMessage(string text, string fromNode)
    {
        if (TextMessageReceived != null)
        {
            await TextMessageReceived.Invoke(text, fromNode);
        }
    }
}
