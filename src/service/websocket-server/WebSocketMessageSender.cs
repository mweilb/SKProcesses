using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketMessageSender
{
    private readonly WebSocket _webSocket;

    public WebSocketMessageSender(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public async Task SendStateAsync(object? state, string message, string? eventId = null)
    {
        var camelOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "state_update",
            state,
            message,
            eventId
        }, camelOptions);

        await _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(payload),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    public async Task SendActiveStateAsync(string activate, string from)
    {
        var camelOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "active_state",
            activate,
            from
        }, camelOptions);

        // Debug print: print the outgoing activate/from pair
        Console.WriteLine("SendActiveStateAsync outgoing payload:");
        Console.WriteLine($"[DEBUG] SendActiveStateAsync trail: activate={activate}, from={from}");

        await _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(payload),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    public async Task SendMermaidAsync(string? mermaid, object? nodes, object? edges, string? error = null)
    {
        var camelOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "mermaid",
            mermaid = mermaid ?? "",
            nodes = nodes ?? new object[0],
            edges = edges ?? new object[0],
            error
        }, camelOptions);

        await _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(payload),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}
