using Familiar.Meshtastic;

namespace Familiar.Host.Endpoints;

public static class MeshtasticEndpoints
{
    public static void MapMeshtasticEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/meshtastic");

        group.MapGet("/status", (MeshtasticService service) =>
        {
            return Results.Ok(new
            {
                Connected = service.Client.IsConnected,
                NodeCount = service.Client.KnownNodes.Count
            });
        });

        group.MapGet("/nodes", (MeshtasticService service) =>
        {
            var nodes = service.Client.KnownNodes.Select(n => new
            {
                n.NodeId,
                n.Name,
                n.ShortName,
                n.LastHeard,
                n.BatteryLevel
            });

            return Results.Ok(new { Nodes = nodes });
        });

        group.MapPost("/send", async (
            SendMessageRequest request,
            MeshtasticService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new { Error = "Text is required" });
            }

            if (!service.Client.IsConnected)
            {
                return Results.BadRequest(new { Error = "Not connected to Meshtastic" });
            }

            await service.Client.SendMessageAsync(request.Text, request.DestinationNode, ct);
            return Results.Ok(new { Sent = true });
        });
    }

    public record SendMessageRequest(string Text, string? DestinationNode = null);
}
