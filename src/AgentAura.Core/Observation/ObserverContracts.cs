using AgentAura.Core.Items;

namespace AgentAura.Core.Observation;

public sealed record ObservedThreadSnapshot(
    string ThreadId,
    string WorkingDirectory,
    string? CodexThreadTitle,
    string? CurrentTurnId,
    AgentMessageItemState State);

public sealed record ObserverSnapshot(long Epoch, IReadOnlyList<ObservedThreadSnapshot> Threads);

public abstract record ObserverEvent(long Epoch);
public sealed record ObserverInputReceived(long Epoch, ThreadEvent Input) : ObserverEvent(Epoch);
public sealed record ObserverTransportLost(long Epoch, string Diagnostic) : ObserverEvent(Epoch);

public interface ICodexAppServerObserver : IAsyncDisposable
{
    long ConnectionEpoch { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<ObserverSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ObserverEvent> ObserveAsync(CancellationToken cancellationToken = default);
}
