namespace AgentAura.Core.Items;

public sealed class AgentMessageItemStore : IThreadStateReducer
{
    private readonly AttentionPinSpan _attentionPinSpan;
    private readonly Dictionary<string, AgentMessageItem> _items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (long Epoch, long Sequence)> _lastAccepted = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _completedTurnIds = new(StringComparer.Ordinal);

    public AgentMessageItemStore(AttentionPinSpan attentionPinSpan)
    {
        _attentionPinSpan = attentionPinSpan;
    }

    public IReadOnlyList<AgentMessageItem> Items => _items.Values
        .OrderByDescending(item => item.IsAttentionPinned)
        .ThenByDescending(item => item.LastStateChangeAt)
        .ThenBy(item => item.ThreadId, StringComparer.Ordinal)
        .ToArray();

    public ApplyResult Apply(ThreadEvent input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Validate();
        if (IsStaleOrDuplicate(input)) return new(false, false, GetItem(input.ThreadId));
        if (input is TurnCompleted completed && IsDuplicateTerminalResult(completed)) return new(false, false, GetItem(input.ThreadId));

        var current = GetItem(input.ThreadId) ?? CreateObservedItem(input);
        if (IsForAnotherTurn(input, current)) return new(false, false, GetItem(input.ThreadId));
        var (next, significant) = Reduce(current, input);
        _lastAccepted[input.ThreadId] = (input.Epoch, input.Sequence);
        if (input is TurnCompleted terminal) RememberTerminalResult(terminal);
        if (significant) DecrementOtherAttentionPins(input.ThreadId);
        _items[input.ThreadId] = next;
        return new(true, significant, next);
    }

    public bool Dismiss(string threadId) => _items.Remove(threadId);

    public void Clear() => _items.Clear();

    public void SetThreadAlias(string threadId, string? alias)
    {
        if (string.IsNullOrWhiteSpace(threadId)) throw new ArgumentException("A stable Codex Thread ID is required.", nameof(threadId));
        if (string.IsNullOrWhiteSpace(alias)) _aliases.Remove(threadId);
        else _aliases[threadId] = alias.Trim();
        if (_items.TryGetValue(threadId, out var item)) _items[threadId] = item with { ThreadAlias = GetAlias(threadId) };
    }

    private bool IsStaleOrDuplicate(ThreadEvent input) => _lastAccepted.TryGetValue(input.ThreadId, out var prior) &&
        (input.Epoch < prior.Epoch || input.Epoch == prior.Epoch && input.Sequence <= prior.Sequence);

    private AgentMessageItem? GetItem(string threadId) => _items.GetValueOrDefault(threadId);

    private AgentMessageItem CreateObservedItem(ThreadEvent input)
    {
        var workingDirectory = input is ThreadObserved observed ? observed.WorkingDirectory : string.Empty;
        var title = input is ThreadObserved titleObserved ? titleObserved.CodexThreadTitle : null;
        return new AgentMessageItem(input.ThreadId, workingDirectory, title, GetAlias(input.ThreadId), FallbackTitle(workingDirectory, input.OccurredAt),
            AgentMessageItemState.Observed, null, null, input.OccurredAt, input.Epoch, 0, null);
    }

    private (AgentMessageItem Item, bool Significant) Reduce(AgentMessageItem item, ThreadEvent input) => input switch
    {
        ThreadObserved observed => (item with { WorkingDirectory = observed.WorkingDirectory, CodexThreadTitle = observed.CodexThreadTitle ?? item.CodexThreadTitle, Epoch = observed.Epoch }, false),
        TurnStarted started => (item with { State = AgentMessageItemState.Running, CurrentTurnId = started.TurnId, LatestTurnOutcome = null, LastStateChangeAt = started.OccurredAt, Epoch = started.Epoch, RemainingAttentionPinUpdates = 0, Detail = null }, true),
        AttentionRequested attention when IsCurrentTurn(item, attention.TurnId) && !HasTerminalOutcome(item) => (item with { State = AgentMessageItemState.Attention, LastStateChangeAt = attention.OccurredAt, Epoch = attention.Epoch, RemainingAttentionPinUpdates = _attentionPinSpan.SignificantUpdates, Detail = attention.Reason }, item.State != AgentMessageItemState.Attention),
        AttentionRequested => (item, false),
        ServerRequestResolved resolved when IsCurrentTurn(item, resolved.TurnId) && item.State == AgentMessageItemState.Attention => (item with { State = AgentMessageItemState.Running, LastStateChangeAt = resolved.OccurredAt, Epoch = resolved.Epoch, RemainingAttentionPinUpdates = 0, Detail = null }, false),
        ServerRequestResolved => (item, false),
        TurnCompleted completed when IsCurrentTurn(item, completed.TurnId) => (item with { State = ToState(completed.Outcome), LatestTurnOutcome = completed.Outcome, LastStateChangeAt = completed.OccurredAt, Epoch = completed.Epoch, RemainingAttentionPinUpdates = 0, Detail = completed.Detail }, true),
        TurnCompleted => (item, false),
        ThreadTitleChanged renamed => (item with { CodexThreadTitle = renamed.CodexThreadTitle, Epoch = renamed.Epoch }, false),
        TransportDisconnected disconnected => (item with { State = AgentMessageItemState.ConnectionDisconnected, LastStateChangeAt = disconnected.OccurredAt, Epoch = disconnected.Epoch, RemainingAttentionPinUpdates = 0, Detail = "Observer connection disconnected; no Turn outcome was inferred." }, false),
        _ => (item, false)
    };

    private void DecrementOtherAttentionPins(string changedThreadId)
    {
        if (_attentionPinSpan.IsAlwaysPinned) return;
        foreach (var (threadId, item) in _items.ToArray())
        {
            if (threadId != changedThreadId && item.State == AgentMessageItemState.Attention && item.RemainingAttentionPinUpdates is > 0)
                _items[threadId] = item with { RemainingAttentionPinUpdates = item.RemainingAttentionPinUpdates - 1 };
        }
    }

    private string? GetAlias(string threadId) => _aliases.GetValueOrDefault(threadId);
    private static bool IsCurrentTurn(AgentMessageItem item, string turnId) => string.IsNullOrWhiteSpace(item.CurrentTurnId) || item.CurrentTurnId == turnId;
    private static bool IsForAnotherTurn(ThreadEvent input, AgentMessageItem item) => input switch
    {
        AttentionRequested attention => !IsCurrentTurn(item, attention.TurnId),
        ServerRequestResolved resolved => !IsCurrentTurn(item, resolved.TurnId),
        TurnCompleted completed => !IsCurrentTurn(item, completed.TurnId),
        _ => false
    };
    private static bool HasTerminalOutcome(AgentMessageItem item) => item.LatestTurnOutcome is not null && item.CurrentTurnId is not null;
    private bool IsDuplicateTerminalResult(TurnCompleted completed) => _completedTurnIds.TryGetValue(completed.ThreadId, out var completedTurns) && completedTurns.Contains(completed.TurnId);
    private void RememberTerminalResult(TurnCompleted completed) => (_completedTurnIds.TryGetValue(completed.ThreadId, out var completedTurns) ? completedTurns : _completedTurnIds[completed.ThreadId] = new(StringComparer.Ordinal)).Add(completed.TurnId);
    private static AgentMessageItemState ToState(TurnOutcomeState outcome) => outcome switch { TurnOutcomeState.Completed => AgentMessageItemState.Completed, TurnOutcomeState.Failed => AgentMessageItemState.Failed, TurnOutcomeState.Interrupted => AgentMessageItemState.Interrupted, _ => throw new ArgumentOutOfRangeException(nameof(outcome)) };
    private static string FallbackTitle(string workingDirectory, DateTimeOffset observedAt)
    {
        var normalized = workingDirectory.Replace('\\', '/');
        var trimmed = normalized.TrimEnd('/');
        var leaf = trimmed[(trimmed.LastIndexOf('/') + 1)..];
        return $"{(string.IsNullOrWhiteSpace(leaf) ? "Unknown project" : leaf)} · {observedAt:yyyy-MM-dd HH:mm}";
    }
}
