namespace AgentAura.Core.Items;

public sealed record AgentMessageItem(
    string ThreadId,
    string WorkingDirectory,
    string? CodexThreadTitle,
    string? ThreadAlias,
    string FallbackThreadTitle,
    AgentMessageItemState State,
    string? CurrentTurnId,
    TurnOutcomeState? LatestTurnOutcome,
    DateTimeOffset LastStateChangeAt,
    long Epoch,
    int? RemainingAttentionPinUpdates,
    string? Detail)
{
    public string DisplayTitle => ThreadAlias ?? CodexThreadTitle ?? FallbackThreadTitle;
    public bool IsAttentionPinned => State == AgentMessageItemState.Attention && RemainingAttentionPinUpdates is not 0;
}

public sealed record ApplyResult(bool Applied, bool IsSignificantUpdate, AgentMessageItem? Item);

public interface IThreadStateReducer
{
    ApplyResult Apply(ThreadEvent input);
}
