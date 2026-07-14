namespace AgentAura.Core.Items;

public abstract record ThreadEvent(long Epoch, long Sequence, string ThreadId, DateTimeOffset OccurredAt)
{
    public void Validate()
    {
        if (Epoch < 1) throw new ArgumentOutOfRangeException(nameof(Epoch));
        if (Sequence < 1) throw new ArgumentOutOfRangeException(nameof(Sequence));
        if (string.IsNullOrWhiteSpace(ThreadId)) throw new ArgumentException("A stable Codex Thread ID is required.", nameof(ThreadId));
    }
}

public sealed record ThreadObserved(long Epoch, long Sequence, string ThreadId, string WorkingDirectory, string? CodexThreadTitle, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt)
{
    public ThreadObserved(long Epoch, string ThreadId, string WorkingDirectory, string? CodexThreadTitle, DateTimeOffset OccurredAt)
        : this(Epoch, 1, ThreadId, WorkingDirectory, CodexThreadTitle, OccurredAt) { }
}

public sealed record TurnStarted(long Epoch, long Sequence, string ThreadId, string TurnId, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public sealed record AttentionRequested(long Epoch, long Sequence, string ThreadId, string TurnId, string? Reason, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public sealed record ServerRequestResolved(long Epoch, long Sequence, string ThreadId, string TurnId, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public sealed record TurnCompleted(long Epoch, long Sequence, string ThreadId, string TurnId, TurnOutcomeState Outcome, string? Detail, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public sealed record ThreadTitleChanged(long Epoch, long Sequence, string ThreadId, string? CodexThreadTitle, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public sealed record TransportDisconnected(long Epoch, long Sequence, string ThreadId, DateTimeOffset OccurredAt)
    : ThreadEvent(Epoch, Sequence, ThreadId, OccurredAt);

public enum TurnOutcomeState { Completed, Failed, Interrupted }
