using AgentAura.Prototype.Models;

namespace AgentAura.Prototype.BridgePrototype;

// PROTOTYPE: pure protocol-to-view-state reducer. Delete or absorb after ticket 05 is resolved.
internal sealed record BridgeThreadState(
    string ThreadId,
    string? Name,
    string WorkingDirectory,
    AgentItemState State,
    string Detail)
{
    public static BridgeThreadState Empty(string threadId) =>
        new(threadId, null, string.Empty, AgentItemState.Observed, "Thread discovered through App Server.");

    public AgentItemSample ToAgentItem(string? alias = null)
    {
        var project = string.IsNullOrWhiteSpace(WorkingDirectory)
            ? "Unknown project"
            : Path.GetFileName(WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var title = !string.IsNullOrWhiteSpace(alias)
            ? alias
            : string.IsNullOrWhiteSpace(Name)
                ? $"Codex Thread {ThreadId[..Math.Min(8, ThreadId.Length)]}"
                : Name;
        return new AgentItemSample(State, title, project, WorkingDirectory, Detail, ThreadId);
    }
}

internal static class BridgeThreadReducer
{
    public static BridgeThreadState Reduce(BridgeThreadState? current, string method, JsonElement payload)
    {
        var threadId = ReadThreadId(method, payload) ?? current?.ThreadId
            ?? throw new InvalidOperationException($"{method} did not identify a Thread.");
        var next = current ?? BridgeThreadState.Empty(threadId);

        return method switch
        {
            "thread/resumed" or "thread/started" => ReduceThread(next, ReadThreadObject(method, payload)),
            "thread/status/changed" => ReduceStatus(next, payload.GetProperty("status")),
            "thread/name/updated" => next with
            {
                Name = payload.TryGetProperty("threadName", out var name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString()
                    : null,
                Detail = "Codex Thread title updated."
            },
            "turn/started" => next with { State = AgentItemState.Running, Detail = "Turn started." },
            "turn/completed" => ReduceCompletedTurn(next, payload.GetProperty("turn")),
            "transport/disconnected" => next with
            {
                State = AgentItemState.Disconnected,
                Detail = "Observer connection disconnected; no Turn outcome was inferred."
            },
            _ => next
        };
    }

    private static BridgeThreadState ReduceThread(BridgeThreadState current, JsonElement thread)
    {
        var next = current with
        {
            Name = ReadOptionalString(thread, "name") ?? current.Name,
            WorkingDirectory = ReadOptionalString(thread, "cwd") ?? current.WorkingDirectory,
            Detail = "Observer joined the App Server Thread."
        };

        if (thread.TryGetProperty("turns", out var turns) && turns.ValueKind == JsonValueKind.Array)
        {
            var lastTurn = turns.EnumerateArray().LastOrDefault();
            if (lastTurn.ValueKind == JsonValueKind.Object && lastTurn.TryGetProperty("status", out _))
            {
                next = ReduceCompletedTurn(next, lastTurn);
            }
        }

        return thread.TryGetProperty("status", out var status) ? ReduceStatus(next, status) : next;
    }

    private static BridgeThreadState ReduceStatus(BridgeThreadState current, JsonElement status)
    {
        var type = ReadOptionalString(status, "type");
        if (type == "active")
        {
            var flags = status.TryGetProperty("activeFlags", out var activeFlags) && activeFlags.ValueKind == JsonValueKind.Array
                ? activeFlags.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null).ToArray()
                : [];
            var waiting = flags.Contains("waitingOnApproval") || flags.Contains("waitingOnUserInput");
            return current with
            {
                State = waiting ? AgentItemState.Attention : AgentItemState.Running,
                Detail = waiting ? $"Waiting: {string.Join(", ", flags)}" : "Thread is active."
            };
        }

        if (type == "systemError")
        {
            return current with { Detail = "App Server reported a Thread system error." };
        }

        // Idle/notLoaded never overwrites an exact completed/failed/interrupted outcome.
        return current.State is AgentItemState.Completed or AgentItemState.Failed or AgentItemState.Interrupted
            ? current
            : current with { State = AgentItemState.Observed, Detail = $"Thread status: {type ?? "unknown"}." };
    }

    private static BridgeThreadState ReduceCompletedTurn(BridgeThreadState current, JsonElement turn)
    {
        var status = ReadOptionalString(turn, "status");
        return status switch
        {
            "completed" => current with { State = AgentItemState.Completed, Detail = "Turn completed." },
            "failed" => current with { State = AgentItemState.Failed, Detail = ReadTurnError(turn) ?? "Turn failed." },
            "interrupted" => current with { State = AgentItemState.Interrupted, Detail = "Turn interrupted." },
            "inProgress" => current with { State = AgentItemState.Running, Detail = "Turn is in progress." },
            _ => current
        };
    }

    private static string? ReadThreadId(string method, JsonElement payload)
    {
        if (payload.TryGetProperty("threadId", out var threadId) && threadId.ValueKind == JsonValueKind.String)
        {
            return threadId.GetString();
        }

        var thread = ReadThreadObject(method, payload);
        return thread.ValueKind == JsonValueKind.Object ? ReadOptionalString(thread, "id") : null;
    }

    private static JsonElement ReadThreadObject(string method, JsonElement payload)
    {
        if (method == "thread/started" && payload.TryGetProperty("thread", out var notificationThread))
        {
            return notificationThread;
        }

        if (method == "thread/resumed" && payload.TryGetProperty("thread", out var responseThread))
        {
            return responseThread;
        }

        return default;
    }

    private static string? ReadTurnError(JsonElement turn)
    {
        if (!turn.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadOptionalString(error, "message");
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
