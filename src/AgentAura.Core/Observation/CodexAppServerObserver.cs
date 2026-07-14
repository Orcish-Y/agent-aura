using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AgentAura.Core.Items;

namespace AgentAura.Core.Observation;

public interface IAppServerConnection : IAsyncDisposable
{
    Task SendAsync(string message, CancellationToken cancellationToken);
    IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken);
}

public interface IAppServerConnectionFactory
{
    Task<IAppServerConnection> ConnectAsync(Uri endpoint, CancellationToken cancellationToken);
}

public sealed class WebSocketAppServerConnectionFactory : IAppServerConnectionFactory
{
    public async Task<IAppServerConnection> ConnectAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(endpoint, cancellationToken);
        return new WebSocketAppServerConnection(socket);
    }
}

/// <summary>Observes only the documented App Server events required by Agent Aura.</summary>
public sealed class CodexAppServerObserver : ICodexAppServerObserver
{
    private readonly Uri _endpoint;
    private readonly IAppServerConnectionFactory _connections;
    private readonly Channel<ObserverEvent> _events = Channel.CreateUnbounded<ObserverEvent>();
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly Dictionary<string, ObservedThreadSnapshot> _threads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _currentTurnIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _sequences = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _stopping = new();
    private IAppServerConnection? _connection;
    private Task? _receiver;
    private long _nextRequestId;
    private bool _initialized;
    private bool _transportLost;

    public CodexAppServerObserver(Uri endpoint, IAppServerConnectionFactory? connections = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (endpoint.Scheme is not "ws" and not "wss") throw new ArgumentException("The App Server endpoint must be a WebSocket URI.", nameof(endpoint));
        _endpoint = endpoint;
        _connections = connections ?? new WebSocketAppServerConnectionFactory();
    }

    public long ConnectionEpoch { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized && !_transportLost) throw new InvalidOperationException("This observer connection has already been initialized.");
        if (_connection is not null) await _connection.DisposeAsync();
        _connection = await _connections.ConnectAsync(_endpoint, cancellationToken);
        ConnectionEpoch++;
        _threads.Clear();
        _currentTurnIds.Clear();
        _sequences.Clear();
        _transportLost = false;
        _receiver = ReceiveAsync(ConnectionEpoch, _stopping.Token);
        await RequestAsync("initialize", new { clientInfo = new { name = "agent_aura", title = "Agent Aura", version = "0.1.0" }, capabilities = new { optOutNotificationMethods = NoisyNotifications } }, cancellationToken);
        await NotifyAsync("initialized", new { }, cancellationToken);
        _initialized = true;
    }

    public async Task<ObserverSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        var loaded = await RequestAsync("thread/loaded/list", new { }, cancellationToken);
        if (loaded.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var id in data.EnumerateArray().Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>())
            {
                var resumed = await RequestAsync("thread/resume", new { threadId = id, excludeTurns = false }, cancellationToken);
                ApplyResumedSnapshot(resumed);
            }
        }

        return new ObserverSnapshot(ConnectionEpoch, _threads.Values.OrderBy(thread => thread.ThreadId, StringComparer.Ordinal).ToArray());
    }

    public IAsyncEnumerable<ObserverEvent> ObserveAsync(CancellationToken cancellationToken = default) => _events.Reader.ReadAllAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _stopping.Cancel();
        _events.Writer.TryComplete();
        if (_receiver is not null)
        {
            try { await _receiver; } catch (OperationCanceledException) { }
        }
        if (_connection is not null) await _connection.DisposeAsync();
        _stopping.Dispose();
    }

    private async Task ReceiveAsync(long epoch, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _connection!.ReadAllAsync(cancellationToken))
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;
                if (root.TryGetProperty("id", out var id) && id.TryGetInt64(out var requestId) && !root.TryGetProperty("method", out _))
                {
                    if (_pending.TryRemove(requestId, out var completion))
                    {
                        if (root.TryGetProperty("error", out var error)) completion.TrySetException(new InvalidOperationException(error.ToString()));
                        else completion.TrySetResult(root.GetProperty("result").Clone());
                    }
                    continue;
                }
                if (root.TryGetProperty("method", out var method) && root.TryGetProperty("params", out var parameters))
                    if (epoch == ConnectionEpoch) ApplyProtocolEvent(method.GetString() ?? string.Empty, parameters);
            }
            MarkTransportLost(epoch, "The App Server closed the observer connection.");
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested) { }
        catch (Exception exception) { MarkTransportLost(epoch, exception.Message); }
    }

    private void ApplyProtocolEvent(string method, JsonElement parameters, bool publish = true)
    {
        var threadId = ReadThreadId(parameters);
        var knownTurnId = threadId is not null && _currentTurnIds.TryGetValue(threadId, out var currentTurnId) ? currentTurnId : null;
        if (threadId is not null)
        {
            var input = CodexAppServerProtocol.ToThreadEvent(ConnectionEpoch, NextSequence(threadId), method, parameters, DateTimeOffset.UtcNow, knownTurnId);
            if (input is not null)
            {
                UpdateSnapshot(input);
                if (publish) _events.Writer.TryWrite(new ObserverInputReceived(ConnectionEpoch, input));
            }
        }
    }

    private void ApplyResumedSnapshot(JsonElement resumed)
    {
        ApplyProtocolEvent("thread/resumed", resumed, publish: false);
        if (!resumed.TryGetProperty("thread", out var thread) || thread.ValueKind != JsonValueKind.Object ||
            !thread.TryGetProperty("turns", out var turns) || turns.ValueKind != JsonValueKind.Array)
            return;

        var latestTurn = turns.EnumerateArray().LastOrDefault();
        if (latestTurn.ValueKind != JsonValueKind.Object || !latestTurn.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String)
            return;

        using var parameters = JsonDocument.Parse($"{{\"turn\":{latestTurn.GetRawText()}}}");
        ApplyProtocolEvent(status.GetString() == "inProgress" ? "turn/started" : "turn/completed", parameters.RootElement, publish: false);
    }

    private void UpdateSnapshot(ThreadEvent input)
    {
        var current = _threads.GetValueOrDefault(input.ThreadId) ?? new ObservedThreadSnapshot(input.ThreadId, string.Empty, null, null, AgentMessageItemState.Observed);
        _threads[input.ThreadId] = input switch
        {
            ThreadObserved observed => current with { WorkingDirectory = observed.WorkingDirectory, CodexThreadTitle = observed.CodexThreadTitle ?? current.CodexThreadTitle },
            TurnStarted started => SetCurrentTurn(current, started.TurnId, AgentMessageItemState.Running),
            AttentionRequested attention => SetCurrentTurn(current, attention.TurnId, AgentMessageItemState.Attention),
            ServerRequestResolved => current with { State = AgentMessageItemState.Running },
            TurnCompleted completed => SetCurrentTurn(current, completed.TurnId, completed.Outcome switch { TurnOutcomeState.Completed => AgentMessageItemState.Completed, TurnOutcomeState.Failed => AgentMessageItemState.Failed, _ => AgentMessageItemState.Interrupted }),
            ThreadTitleChanged renamed => current with { CodexThreadTitle = renamed.CodexThreadTitle },
            _ => current
        };
    }

    private ObservedThreadSnapshot SetCurrentTurn(ObservedThreadSnapshot current, string turnId, AgentMessageItemState state)
    {
        _currentTurnIds[current.ThreadId] = turnId;
        return current with { CurrentTurnId = turnId, State = state };
    }

    private void MarkTransportLost(long epoch, string diagnostic)
    {
        if (epoch != ConnectionEpoch || _transportLost) return;
        _transportLost = true;
        foreach (var thread in _threads.Values)
        {
            var disconnected = new TransportDisconnected(ConnectionEpoch, NextSequence(thread.ThreadId), thread.ThreadId, DateTimeOffset.UtcNow);
            _events.Writer.TryWrite(new ObserverInputReceived(ConnectionEpoch, disconnected));
        }
        _events.Writer.TryWrite(new ObserverTransportLost(ConnectionEpoch, diagnostic));
    }

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private async Task<JsonElement> RequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextRequestId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        await SendAsync(new { method, id, @params = parameters }, cancellationToken);
        return await completion.Task.WaitAsync(RequestTimeout, cancellationToken);
    }

    private Task NotifyAsync(string method, object parameters, CancellationToken cancellationToken) => SendAsync(new { method, @params = parameters }, cancellationToken);
    private Task SendAsync(object message, CancellationToken cancellationToken) => _connection!.SendAsync(JsonSerializer.Serialize(message), cancellationToken);
    private long NextSequence(string threadId) => _sequences.TryGetValue(threadId, out var sequence) ? _sequences[threadId] = sequence + 1 : _sequences[threadId] = 1;
    private void EnsureInitialized() { if (!_initialized) throw new InvalidOperationException("InitializeAsync must complete before observing the App Server."); }
    private static string? ReadThreadId(JsonElement parameters) => parameters.TryGetProperty("threadId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : parameters.TryGetProperty("thread", out var thread) && thread.TryGetProperty("id", out var threadId) ? threadId.GetString() : parameters.TryGetProperty("turn", out var turn) && turn.TryGetProperty("threadId", out var turnThreadId) ? turnThreadId.GetString() : null;
    private static readonly string[] NoisyNotifications = ["item/agentMessage/delta", "item/reasoning/summaryTextDelta", "item/reasoning/textDelta", "item/commandExecution/outputDelta", "item/fileChange/outputDelta"];
}

internal sealed class WebSocketAppServerConnection(ClientWebSocket socket) : IAppServerConnection
{
    public Task SendAsync(string message, CancellationToken cancellationToken) => socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);

    public async IAsyncEnumerable<string> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[32 * 1024];
        while (socket.State == WebSocketState.Open)
        {
            using var payload = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) yield break;
                payload.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            yield return Encoding.UTF8.GetString(payload.ToArray());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent Aura exiting", CancellationToken.None); } catch (WebSocketException) { }
        }
        socket.Dispose();
    }
}
