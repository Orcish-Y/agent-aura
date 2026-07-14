using System.Text.Json;
using AgentAura.Core.Items;

namespace AgentAura.Core.Observation;

/// <summary>Maps the supported App Server notification subset into Agent Aura's durable domain contract.</summary>
public static class CodexAppServerProtocol
{
    public static ThreadEvent? ToThreadEvent(
        long epoch,
        long sequence,
        string method,
        string parameters,
        DateTimeOffset occurredAt,
        string? knownTurnId)
    {
        using var document = JsonDocument.Parse(parameters);
        return ToThreadEvent(epoch, sequence, method, document.RootElement, occurredAt, knownTurnId);
    }

    public static ThreadEvent? ToThreadEvent(
        long epoch,
        long sequence,
        string method,
        JsonElement parameters,
        DateTimeOffset occurredAt,
        string? knownTurnId)
    {
        var threadId = ReadString(parameters, "threadId") ?? ReadString(GetObject(parameters, "thread"), "id") ?? ReadString(GetObject(parameters, "turn"), "threadId");
        if (string.IsNullOrWhiteSpace(threadId)) return null;

        return method switch
        {
            "thread/started" or "thread/resumed" => ToObserved(epoch, sequence, threadId, parameters, occurredAt),
            "turn/started" => ToTurnStarted(epoch, sequence, threadId, parameters, occurredAt),
            "turn/completed" => ToTurnCompleted(epoch, sequence, threadId, parameters, occurredAt),
            "thread/name/updated" => new ThreadTitleChanged(epoch, sequence, threadId, ReadString(parameters, "threadName"), occurredAt),
            "item/commandExecution/requestApproval" or "item/fileChange/requestApproval" or "item/permissions/requestApproval" or "tool/requestUserInput" or "mcpServer/elicitation/request" =>
                ToAttention(epoch, sequence, threadId, parameters, occurredAt, knownTurnId),
            "serverRequest/resolved" => ToRequestResolved(epoch, sequence, threadId, parameters, occurredAt, knownTurnId),
            "thread/status/changed" => ToStatus(epoch, sequence, threadId, parameters, occurredAt, knownTurnId),
            _ => null
        };
    }

    private static ThreadEvent ToObserved(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt)
    {
        var thread = GetObject(parameters, "thread");
        return new ThreadObserved(epoch, sequence, threadId, ReadString(thread, "cwd") ?? string.Empty, ReadString(thread, "name"), occurredAt);
    }

    private static ThreadEvent? ToTurnStarted(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt)
    {
        var turn = GetObject(parameters, "turn");
        var turnId = ReadString(turn, "id") ?? ReadString(parameters, "turnId");
        return string.IsNullOrWhiteSpace(turnId) ? null : new TurnStarted(epoch, sequence, threadId, turnId, occurredAt);
    }

    private static ThreadEvent? ToTurnCompleted(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt)
    {
        var turn = GetObject(parameters, "turn");
        var turnId = ReadString(turn, "id") ?? ReadString(parameters, "turnId");
        var outcome = ReadString(turn, "status") switch
        {
            "completed" => TurnOutcomeState.Completed,
            "failed" => TurnOutcomeState.Failed,
            "interrupted" => TurnOutcomeState.Interrupted,
            _ => (TurnOutcomeState?)null
        };
        return string.IsNullOrWhiteSpace(turnId) || outcome is null ? null : new TurnCompleted(epoch, sequence, threadId, turnId, outcome.Value, ReadString(GetObject(turn, "error"), "message"), occurredAt);
    }

    private static ThreadEvent? ToAttention(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt, string? knownTurnId)
    {
        var turnId = ReadString(parameters, "turnId") ?? knownTurnId;
        return string.IsNullOrWhiteSpace(turnId) ? null : new AttentionRequested(epoch, sequence, threadId, turnId, ReadString(parameters, "reason") ?? ReadString(parameters, "message"), occurredAt);
    }

    private static ThreadEvent? ToRequestResolved(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt, string? knownTurnId) =>
        string.IsNullOrWhiteSpace(knownTurnId) ? null : new ServerRequestResolved(epoch, sequence, threadId, knownTurnId, occurredAt);

    private static ThreadEvent? ToStatus(long epoch, long sequence, string threadId, JsonElement parameters, DateTimeOffset occurredAt, string? knownTurnId)
    {
        var status = GetObject(parameters, "status");
        var activeFlags = status.TryGetProperty("activeFlags", out var flags) && flags.ValueKind == JsonValueKind.Array
            ? flags.EnumerateArray().Select(flag => flag.GetString()).ToArray()
            : [];
        var waiting = activeFlags.Contains("waitingOnApproval", StringComparer.Ordinal) || activeFlags.Contains("waitingOnUserInput", StringComparer.Ordinal);
        return waiting && !string.IsNullOrWhiteSpace(knownTurnId)
            ? new AttentionRequested(epoch, sequence, threadId, knownTurnId, string.Join(", ", activeFlags), occurredAt)
            : null;
    }

    private static JsonElement GetObject(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object ? child : default;

    private static string? ReadString(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.String ? child.GetString() : null;
}
