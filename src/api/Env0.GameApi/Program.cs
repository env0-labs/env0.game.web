using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Env0.Core;
using env0.maintenance;
using env0.records;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameSessions>();

var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNameCaseInsensitive = true
};

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { ok = true }));

app.MapGet("/", () => Results.Ok(new
{
    name = "env0.game.web",
    mode = "thin-client",
    transport = "websocket",
    endpoints = new[] { "/ws" }
}));

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.Map("/ws", async (HttpContext context, GameSessions sessions) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
        return Results.BadRequest("Expected WebSocket");

    using var socket = await context.WebSockets.AcceptWebSocketAsync();

    var session = sessions.Create();
    await SendAsync(socket, new ServerMessage("session", new { sessionId = session.Id }));

    // Send initial banner/output
    await SendSessionOutputAsync(socket, session, input: string.Empty);

    var buffer = new byte[16 * 1024];
    while (socket.State == WebSocketState.Open)
    {
        var recv = await socket.ReceiveAsync(buffer, context.RequestAborted);
        if (recv.MessageType == WebSocketMessageType.Close)
            break;

        var text = Encoding.UTF8.GetString(buffer, 0, recv.Count);
        if (string.IsNullOrWhiteSpace(text))
            continue;

        ClientMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<ClientMessage>(text, jsonOpts);
        }
        catch
        {
            await SendAsync(socket, new ServerMessage("error", new { message = "Invalid JSON" }));
            continue;
        }

        if (msg == null)
            continue;

        if (string.Equals(msg.Type, "input", StringComparison.OrdinalIgnoreCase))
        {
            await SendSessionOutputAsync(socket, session, msg.Text ?? string.Empty);
        }
        else if (string.Equals(msg.Type, "reset", StringComparison.OrdinalIgnoreCase))
        {
            sessions.Remove(session.Id);
            session = sessions.Create();
            await SendAsync(socket, new ServerMessage("session", new { sessionId = session.Id }));
            await SendSessionOutputAsync(socket, session, input: string.Empty);
        }
        else
        {
            await SendAsync(socket, new ServerMessage("error", new { message = $"Unknown type: {msg.Type}" }));
        }
    }

    if (socket.State == WebSocketState.Open)
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", context.RequestAborted);

    sessions.Remove(session.Id);
    return Results.Empty;
});

app.Run();

Task SendAsync(WebSocket socket, object payload, CancellationToken ct = default)
{
    var json = JsonSerializer.Serialize(payload, jsonOpts);
    var bytes = Encoding.UTF8.GetBytes(json);
    return socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
}

async Task SendSessionOutputAsync(WebSocket socket, GameSession session, string input)
{
    // step current module
    var lines = session.Step(input);
    await SendAsync(socket, new ServerMessage("output", new { lines }), CancellationToken.None);

    // if the module requested a route change, swap module and emit its initial output
    while (session.State.IsComplete && session.State.NextContext != ContextRoute.None)
    {
        var next = session.State.NextContext;
        session.State.NextContext = ContextRoute.None;
        session.State.IsComplete = false;
        session.SwitchTo(next);

        var routedLines = session.Step(string.Empty);
        await SendAsync(socket, new ServerMessage("output", new { lines = routedLines }), CancellationToken.None);
    }
}

record ClientMessage(string Type, string? Text);
record ServerMessage(string Type, object Data);

sealed class GameSessions
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession Create()
    {
        var id = Guid.NewGuid().ToString("n");
        var session = new GameSession(id);
        _sessions[id] = session;
        return session;
    }

    public bool Remove(string id) => _sessions.TryRemove(id, out _);
}

sealed class GameSession
{
    public string Id { get; }
    public SessionState State { get; } = new();

    private readonly RecordsModule _recordsModule = new();
    private IContextModule _module;

    public GameSession(string id)
    {
        Id = id;
        _module = new MaintenanceModule();
    }

    public IReadOnlyList<OutputLine> Step(string input)
    {
        var output = _module.Handle(input, State);
        // normalize to list for serialization
        return output?.ToList() ?? new List<OutputLine>();
    }

    public void SwitchTo(ContextRoute route)
    {
        _module = route switch
        {
            ContextRoute.Maintenance => new MaintenanceModule(),
            ContextRoute.Records => _recordsModule,
            _ => new MaintenanceModule()
        };
    }
}
