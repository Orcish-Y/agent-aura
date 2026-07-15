namespace AgentAura.Core.Guardian;

/// <summary>
/// The clock boundary keeps lease and bounded-drain behaviour deterministic in
/// both the detached Guardian host and its behaviour tests.
/// </summary>
public interface IGuardianClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemGuardianClock : IGuardianClock
{
    public static SystemGuardianClock Instance { get; } = new();
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    private SystemGuardianClock() { }
}

/// <summary>Owns only the managed App Server process; it never owns remote TUIs.</summary>
public interface IGuardianAppServer
{
    bool HasExited { get; }
    Task RequestGracefulShutdownAsync(CancellationToken cancellationToken = default);
    Task ForceTerminateAsync(CancellationToken cancellationToken = default);
}

public sealed record AppServerGuardianOptions(TimeSpan LeaseDuration, TimeSpan GracefulShutdownTimeout)
{
    public static AppServerGuardianOptions Default { get; } = new(TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(5));

    public void Validate()
    {
        if (LeaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(LeaseDuration));
        if (GracefulShutdownTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(GracefulShutdownTimeout));
    }
}

/// <summary>
/// State machine run by the detached Windows App Server Guardian. It drains the
/// managed App Server only after Aura is absent and the last remote TUI leaves.
/// </summary>
public sealed class AppServerGuardian
{
    private readonly IGuardianAppServer _appServer;
    private readonly IGuardianClock _clock;
    private readonly AppServerGuardianOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private FrontEndLease? _lease;
    private int _remoteTuiCount;
    private DateTimeOffset? _forceTerminationAt;
    private bool _gracefulShutdownRequested;
    private bool _forceTerminationRequested;
    private string? _diagnostic;

    public AppServerGuardian(IGuardianAppServer appServer, IGuardianClock? clock = null, AppServerGuardianOptions? options = null)
    {
        _appServer = appServer ?? throw new ArgumentNullException(nameof(appServer));
        _clock = clock ?? SystemGuardianClock.Instance;
        _options = options ?? AppServerGuardianOptions.Default;
        _options.Validate();
    }

    public event EventHandler<int>? RemoteTuiCountChanged;
    public bool IsFrontEndAbsent => _lease is null;

    public async Task<GuardianHealth> StartOrAttachAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return CreateHealth();
        }
        finally { _gate.Release(); }
    }

    public async Task<FrontEndLease> AcquireLeaseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_gracefulShutdownRequested || _appServer.HasExited)
                throw new InvalidOperationException("The App Server Guardian is no longer available for attachment.");

            _lease = NewLease();
            return _lease;
        }
        finally { _gate.Release(); }
    }

    public async Task RenewLeaseAsync(FrontEndLease lease, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RequireCurrentLease(lease);
            _lease = _lease! with { ExpiresAt = _clock.UtcNow + _options.LeaseDuration };
        }
        finally { _gate.Release(); }
    }

    public async Task DetachAsync(FrontEndLease lease, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RequireCurrentLease(lease);
            _lease = null;
            await EvaluateDrainAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task RemoteTuiConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_gracefulShutdownRequested) throw new InvalidOperationException("The App Server Guardian is draining and cannot admit a remote TUI.");
            _remoteTuiCount++;
            RemoteTuiCountChanged?.Invoke(this, _remoteTuiCount);
        }
        finally { _gate.Release(); }
    }

    public async Task RemoteTuiDisconnectedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_remoteTuiCount == 0) return;
            _remoteTuiCount--;
            RemoteTuiCountChanged?.Invoke(this, _remoteTuiCount);
            await EvaluateDrainAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <summary>Called by the detached host's lightweight periodic timer.</summary>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lease is { } lease && lease.ExpiresAt <= _clock.UtcNow) _lease = null;
            await EvaluateDrainAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private FrontEndLease NewLease() => new(Guid.NewGuid().ToString("N"), _clock.UtcNow + _options.LeaseDuration);

    private void RequireCurrentLease(FrontEndLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (_lease is null || _lease.LeaseId != lease.LeaseId || _lease.ExpiresAt <= _clock.UtcNow)
            throw new InvalidOperationException("The Front End Lease is no longer active.");
    }

    private GuardianHealth CreateHealth() => new(!_appServer.HasExited && !_gracefulShutdownRequested, _remoteTuiCount,
        _appServer.HasExited ? "The managed App Server has exited." : _diagnostic ?? (_gracefulShutdownRequested ? "The managed App Server is draining." : null));

    private async Task EvaluateDrainAsync(CancellationToken cancellationToken)
    {
        if (_lease is not null || _remoteTuiCount != 0 || _appServer.HasExited) return;

        if (!_gracefulShutdownRequested)
        {
            _gracefulShutdownRequested = true;
            _forceTerminationAt = _clock.UtcNow + _options.GracefulShutdownTimeout;
            // The graceful request is deliberately not awaited while holding the
            // state-machine lock: a hung child process must not prevent the
            // five-second forced-termination fallback from running.
            _ = ObserveGracefulShutdownAsync();
            return;
        }

        if (!_forceTerminationRequested && _forceTerminationAt <= _clock.UtcNow)
        {
            _forceTerminationRequested = true;
            await _appServer.ForceTerminateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ObserveGracefulShutdownAsync()
    {
        try { await _appServer.RequestGracefulShutdownAsync().ConfigureAwait(false); }
        catch (Exception exception)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { _diagnostic = $"The managed App Server did not shut down gracefully: {exception.Message}"; }
            finally { _gate.Release(); }
        }
    }
}

/// <summary>
/// Front-end-facing adapter. A UI invokes <see cref="BeginDetach"/> and returns
/// immediately; the Guardian handoff continues in the background.
/// </summary>
public sealed class GuardianFrontEndSession : IAsyncDisposable
{
    private readonly AppServerGuardian _guardian;
    private readonly CancellationTokenSource _stopping = new();
    private readonly TimeSpan _heartbeatInterval;
    private FrontEndLease? _lease;
    private Task? _heartbeat;
    private int _disposed;

    public GuardianFrontEndSession(AppServerGuardian guardian, TimeSpan? heartbeatInterval = null)
    {
        _guardian = guardian ?? throw new ArgumentNullException(nameof(guardian));
        _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(15);
        if (_heartbeatInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));
    }

    public async Task StartOrAttachAsync(CancellationToken cancellationToken = default)
    {
        await _guardian.StartOrAttachAsync(cancellationToken).ConfigureAwait(false);
        _lease = await _guardian.AcquireLeaseAsync(cancellationToken).ConfigureAwait(false);
        _heartbeat = HeartbeatAsync(_stopping.Token);
    }

    public void BeginDetach()
    {
        var lease = Interlocked.Exchange(ref _lease, null);
        _stopping.Cancel();
        if (lease is not null) _ = DetachIgnoringCleanupFailureAsync(lease);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return ValueTask.CompletedTask;
        BeginDetach();
        // Disposal can originate on the WPF UI thread. The cancelled heartbeat
        // and detached Guardian handoff deliberately finish off-thread.
        _ = DisposeStoppingSourceAfterHeartbeatAsync();
        return ValueTask.CompletedTask;
    }

    private async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_heartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var lease = _lease;
                if (lease is null) return;
                await _guardian.RenewLeaseAsync(lease, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task DetachIgnoringCleanupFailureAsync(FrontEndLease lease)
    {
        try { await _guardian.DetachAsync(lease).ConfigureAwait(false); }
        catch (Exception) { /* A crashed Guardian will be detected/replaced by the front-end supervisor. */ }
    }

    private async Task DisposeStoppingSourceAfterHeartbeatAsync()
    {
        if (_heartbeat is not null)
        {
            try { await _heartbeat.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _stopping.Dispose();
    }
}
