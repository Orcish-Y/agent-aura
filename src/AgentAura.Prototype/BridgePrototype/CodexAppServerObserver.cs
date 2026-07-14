using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace AgentAura.Prototype.BridgePrototype;

// PROTOTYPE: polling + thread/resume is intentionally concrete so the experiment can prove or reject it.
internal sealed class CodexAppServerObserver : IAsyncDisposable
{
    private readonly Uri _endpoint;
    private readonly string _evidencePath;
    private readonly ClientWebSocket _socket = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly ConcurrentDictionary<long, PendingRequest> _pending = new();
    private readonly Dictionary<string, BridgeThreadState> _threads = [];
    private readonly HashSet<string> _joinedThreadIds = [];
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _logLock = new(1, 1);
    private StreamWriter? _logWriter;
    private long _nextRequestId;
    private Task? _receiveTask;
    private Task? _pollTask;

    public CodexAppServerObserver(Uri endpoint, string evidencePath)
    {
        _endpoint = endpoint;
        _evidencePath = evidencePath;
    }

    public event Action<IReadOnlyCollection<BridgeThreadState>>? SnapshotChanged;
    public event Action<string>? StatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_evidencePath)!);
        _logWriter = new StreamWriter(new FileStream(_evidencePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
        StatusChanged?.Invoke($"Connecting observer to {_endpoint} …");
        await _socket.ConnectAsync(_endpoint, cancellationToken);
        _receiveTask = ReceiveLoopAsync(_stopping.Token);

        await SendRequestAsync(
            "initialize",
            new
            {
                clientInfo = new { name = "agent_aura_bridge_prototype", title = "Agent Aura bridge prototype", version = "0.1.0" },
                capabilities = new
                {
                    optOutNotificationMethods = new[]
                    {
                        "item/agentMessage/delta",
                        "item/reasoning/summaryTextDelta",
                        "item/reasoning/textDelta",
                        "item/commandExecution/outputDelta",
                        "item/fileChange/outputDelta"
                    }
                }
            },
            cancellationToken);
        await SendNotificationAsync("initialized", new { }, cancellationToken);
        await LogAsync(new { kind = "connection", status = "initialized", endpoint = _endpoint.ToString() }, cancellationToken);

        StatusChanged?.Invoke($"Observer connected · polling loaded Threads at {_endpoint}");
        _pollTask = PollLoadedThreadsAsync(_stopping.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _stopping.Cancel();
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent Aura prototype exiting", CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }

        var tasks = new[] { _receiveTask, _pollTask }.Where(task => task is not null).Cast<Task>().ToArray();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
        }

        _socket.Dispose();
        _logWriter?.Dispose();
        _stopping.Dispose();
        _sendLock.Dispose();
        _logLock.Dispose();
    }

    private async Task PollLoadedThreadsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var loaded = await SendRequestAsync("thread/loaded/list", new { limit = 100 }, cancellationToken);
                if (loaded.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var idElement in data.EnumerateArray())
                    {
                        var threadId = idElement.GetString();
                        if (string.IsNullOrWhiteSpace(threadId) || !_joinedThreadIds.Add(threadId))
                        {
                            continue;
                        }

                        try
                        {
                            var resumed = await SendRequestAsync(
                                "thread/resume",
                                new { threadId, excludeTurns = false },
                                cancellationToken);
                            Apply("thread/resumed", resumed);
                            await LogAsync(new { kind = "join", threadId, result = "resumed" }, cancellationToken);
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            _joinedThreadIds.Remove(threadId);
                            await LogAsync(new { kind = "join", threadId, result = "failed", error = exception.Message }, cancellationToken);
                        }
                    }
                }

                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                StatusChanged?.Invoke($"Observer polling error: {exception.Message}");
                await LogAsync(new { kind = "poll-error", error = exception.Message }, cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task<JsonElement> SendRequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextRequestId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = new PendingRequest(method, completion);
        await SendAsync(new { method, id, @params = parameters }, cancellationToken);
        await LogAsync(new { kind = "request", id, method }, cancellationToken);
        return await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private Task SendNotificationAsync(string method, object parameters, CancellationToken cancellationToken) =>
        SendAsync(new { method, @params = parameters }, cancellationToken);

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[32 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        MarkDisconnected("App Server closed the observer connection.");
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                using var document = JsonDocument.Parse(message.ToArray());
                await HandleMessageAsync(document.RootElement, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            MarkDisconnected(exception.Message);
            await LogAsync(new { kind = "receive-error", error = exception.Message }, CancellationToken.None);
        }
    }

    private async Task HandleMessageAsync(JsonElement message, CancellationToken cancellationToken)
    {
        if (message.TryGetProperty("id", out var idElement) && idElement.TryGetInt64(out var id))
        {
            if (message.TryGetProperty("method", out var requestMethod))
            {
                var method = requestMethod.GetString() ?? "unknown";
                await LogAsync(new
                {
                    kind = "server-request",
                    id,
                    method,
                    threadId = ReadOptionalThreadId(message)
                }, cancellationToken);
                StatusChanged?.Invoke($"Server request observed: {method} (left to remote TUI)");
                return;
            }

            if (_pending.TryRemove(id, out var pending))
            {
                if (message.TryGetProperty("error", out var error))
                {
                    var errorText = error.TryGetProperty("message", out var text) ? text.GetString() : error.ToString();
                    pending.Completion.TrySetException(new InvalidOperationException($"{pending.Method}: {errorText}"));
                    await LogAsync(new { kind = "response", id, method = pending.Method, result = "error", error = errorText }, cancellationToken);
                }
                else
                {
                    pending.Completion.TrySetResult(message.GetProperty("result").Clone());
                    await LogAsync(new { kind = "response", id, method = pending.Method, result = "ok" }, cancellationToken);
                }
            }

            return;
        }

        if (!message.TryGetProperty("method", out var methodElement))
        {
            return;
        }

        var notificationMethod = methodElement.GetString() ?? "unknown";
        var payload = message.TryGetProperty("params", out var parameters) ? parameters : default;
        if (notificationMethod is "thread/started" or "thread/status/changed" or "thread/name/updated" or "turn/started" or "turn/completed")
        {
            Apply(notificationMethod, payload);
            await LogRelevantNotificationAsync(notificationMethod, payload, cancellationToken);
        }
    }

    private void Apply(string method, JsonElement payload)
    {
        var threadId = ReadThreadId(payload);
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return;
        }

        _threads.TryGetValue(threadId, out var current);
        _threads[threadId] = BridgeThreadReducer.Reduce(current, method, payload);
        SnapshotChanged?.Invoke(_threads.Values.OrderBy(state => state.ThreadId).ToArray());
    }

    private void MarkDisconnected(string reason)
    {
        foreach (var (threadId, state) in _threads.ToArray())
        {
            using var payload = JsonDocument.Parse($"{{\"threadId\":{JsonSerializer.Serialize(threadId)}}}");
            _threads[threadId] = BridgeThreadReducer.Reduce(state, "transport/disconnected", payload.RootElement);
        }

        SnapshotChanged?.Invoke(_threads.Values.ToArray());
        StatusChanged?.Invoke($"Observer disconnected: {reason}");
    }

    private async Task LogRelevantNotificationAsync(string method, JsonElement payload, CancellationToken cancellationToken)
    {
        var threadId = ReadThreadId(payload);
        string? status = null;
        if (method == "turn/completed" && payload.TryGetProperty("turn", out var turn))
        {
            status = turn.TryGetProperty("status", out var turnStatus) ? turnStatus.GetString() : null;
        }
        else if (method == "thread/status/changed" && payload.TryGetProperty("status", out var threadStatus))
        {
            status = threadStatus.TryGetProperty("type", out var type) ? type.GetString() : null;
        }

        await LogAsync(new { kind = "notification", method, threadId, status }, cancellationToken);
    }

    private async Task LogAsync(object entry, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(new { at = DateTimeOffset.UtcNow, entry });
        await _logLock.WaitAsync(cancellationToken);
        try
        {
            if (_logWriter is not null)
            {
                await _logWriter.WriteLineAsync(line.AsMemory(), cancellationToken);
            }
        }
        finally
        {
            _logLock.Release();
        }
    }

    private static string? ReadThreadId(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (payload.TryGetProperty("threadId", out var threadId) && threadId.ValueKind == JsonValueKind.String)
        {
            return threadId.GetString();
        }

        return payload.TryGetProperty("thread", out var thread) && thread.TryGetProperty("id", out var id)
            ? id.GetString()
            : null;
    }

    private static string? ReadOptionalThreadId(JsonElement message)
    {
        if (!message.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return parameters.TryGetProperty("threadId", out var threadId) ? threadId.GetString() : null;
    }

    private sealed record PendingRequest(string Method, TaskCompletionSource<JsonElement> Completion);
}
