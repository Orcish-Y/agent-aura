using System.Text.Json;
using System.Threading.Channels;
using AgentAura.Core.Items;
using AgentAura.Core.Observation;

var observedAt = new DateTimeOffset(2026, 7, 14, 9, 30, 0, TimeSpan.FromHours(8));
var turnStarted = CodexAppServerProtocol.ToThreadEvent(3, 1, "turn/started", """{"turn":{"id":"turn-3","threadId":"thread-3","status":"inProgress"}}""", observedAt, null);
Assert(turnStarted is TurnStarted { ThreadId: "thread-3", TurnId: "turn-3" }, "turn/started maps to a running Thread event.");

var approval = CodexAppServerProtocol.ToThreadEvent(3, 2, "item/commandExecution/requestApproval", """{"threadId":"thread-3","turnId":"turn-3","reason":"Needs approval"}""", observedAt, "turn-3");
Assert(approval is AttentionRequested { Reason: "Needs approval" }, "An approval request maps to Attention State.");

var terminal = CodexAppServerProtocol.ToThreadEvent(3, 3, "turn/completed", """{"turn":{"id":"turn-3","threadId":"thread-3","status":"failed","error":{"message":"Authoritative error"}}}""", observedAt, "turn-3");
Assert(terminal is TurnCompleted { Outcome: TurnOutcomeState.Failed, Detail: "Authoritative error" }, "turn/completed preserves its exact outcome and diagnostic.");

var renamed = CodexAppServerProtocol.ToThreadEvent(3, 4, "thread/name/updated", """{"threadId":"thread-3","threadName":"Renamed by Codex"}""", observedAt, "turn-3");
Assert(renamed is ThreadTitleChanged { CodexThreadTitle: "Renamed by Codex" }, "A title notification maps to the Codex Thread Title contract.");

var connection = new ScriptedAppServerConnection();
await using var observer = new CodexAppServerObserver(new Uri("ws://127.0.0.1:4500"), new ScriptedConnectionFactory(connection));
await observer.InitializeAsync();
var snapshot = await observer.LoadSnapshotAsync();
Assert(snapshot.Epoch == 1 && snapshot.Threads.Single() is { ThreadId: "thread-4", WorkingDirectory: "/work/agent-aura", CodexThreadTitle: "Observed title", State: AgentMessageItemState.Failed, CurrentTurnId: "turn-3" }, "The observer initializes, discovers loaded Threads, and resumes each Thread into a current snapshot with its exact last outcome.");
await using var observedEvents = observer.ObserveAsync().GetAsyncEnumerator();
connection.Push("""{"method":"turn/started","params":{"turn":{"id":"turn-4","threadId":"thread-4","status":"inProgress"}}}""");
Assert(await observedEvents.MoveNextAsync() && observedEvents.Current is ObserverInputReceived { Input: TurnStarted { ThreadId: "thread-4", TurnId: "turn-4" } }, "The observer maps live App Server events through its public stream.");

var lostConnection = new ScriptedAppServerConnection();
var reconnectedConnection = new ScriptedAppServerConnection();
await using var reconnectingObserver = new CodexAppServerObserver(new Uri("ws://127.0.0.1:4500"), new ScriptedConnectionFactory(lostConnection, reconnectedConnection));
await reconnectingObserver.InitializeAsync();
await reconnectingObserver.LoadSnapshotAsync();
await using var reconnectEvents = reconnectingObserver.ObserveAsync().GetAsyncEnumerator();
lostConnection.Complete();
Assert(await reconnectEvents.MoveNextAsync() && reconnectEvents.Current is ObserverInputReceived { Input: TransportDisconnected { ThreadId: "thread-4", Epoch: 1 } }, "Transport loss emits a disconnected input without inventing a Turn outcome.");
await reconnectingObserver.InitializeAsync();
var reconnectedSnapshot = await reconnectingObserver.LoadSnapshotAsync();
Assert(reconnectedSnapshot.Epoch == 2, "Reconnection creates a fresh epoch and reloads the current App Server snapshot.");

var endpoint = new ManagedCodexAppServerEndpoint("ws://127.0.0.1:4500");
endpoint.Validate();
Assert(endpoint.RemoteTuiCommand == "codex --remote ws://127.0.0.1:4500/", "The managed endpoint exposes the connected-TUI command.");
AssertThrows(() => new ManagedCodexAppServerEndpoint("ws://0.0.0.0:4500").Validate(), "The managed App Server rejects non-loopback listeners.");

Console.WriteLine("PASS: App Server observer protocol, discovery, and endpoint tests.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows(Action action, string message)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new InvalidOperationException(message);
}

sealed class ScriptedConnectionFactory(params ScriptedAppServerConnection[] connections) : IAppServerConnectionFactory
{
    private readonly Queue<ScriptedAppServerConnection> _connections = new(connections);

    public Task<IAppServerConnection> ConnectAsync(Uri endpoint, CancellationToken cancellationToken) => Task.FromResult<IAppServerConnection>(_connections.Dequeue());
}

sealed class ScriptedAppServerConnection : IAppServerConnection
{
    private readonly Channel<string> _messages = Channel.CreateUnbounded<string>();

    public Task SendAsync(string message, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var id)) return Task.CompletedTask;
        var response = root.GetProperty("method").GetString() switch
        {
            "initialize" => "{}",
            "thread/loaded/list" => """{"data":["thread-4"]}""",
            "thread/resume" => """{"thread":{"id":"thread-4","cwd":"/work/agent-aura","name":"Observed title","turns":[{"id":"turn-3","threadId":"thread-4","status":"failed","error":{"message":"Known failure"}}]}}""",
            _ => "{}"
        };
        _messages.Writer.TryWrite($"{{\"id\":{id.GetInt64()},\"result\":{response}}}");
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken) => _messages.Reader.ReadAllAsync(cancellationToken);
    public void Push(string message) => _messages.Writer.TryWrite(message);
    public void Complete() => _messages.Writer.TryComplete();
    public ValueTask DisposeAsync() { _messages.Writer.TryComplete(); return ValueTask.CompletedTask; }
}
