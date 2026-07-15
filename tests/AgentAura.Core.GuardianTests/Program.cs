using AgentAura.Core.Guardian;

var clock = new TestGuardianClock(new DateTimeOffset(2026, 7, 15, 4, 0, 0, TimeSpan.Zero));
var appServer = new TestGuardianAppServer();
var guardian = new AppServerGuardian(appServer, clock);
var lease = await guardian.AcquireLeaseAsync();
await guardian.RemoteTuiConnectedAsync();
await guardian.DetachAsync(lease);
Assert(appServer.GracefulShutdownRequests == 0, "Detaching Aura preserves a connected remote TUI.");
await guardian.RemoteTuiDisconnectedAsync();
Assert(appServer.GracefulShutdownRequests == 1 && appServer.ForceTerminationRequests == 0,
    "The final remote TUI disconnect begins graceful shutdown after Aura detaches.");
clock.Advance(TimeSpan.FromSeconds(5));
await guardian.TickAsync();
Assert(appServer.ForceTerminationRequests == 1, "A graceful shutdown that exceeds five seconds is force terminated.");

var hungShutdown = new TaskCompletionSource();
var hungServer = new TestGuardianAppServer { GracefulShutdown = hungShutdown };
var hungGuardian = new AppServerGuardian(hungServer, clock);
var hungLease = await hungGuardian.AcquireLeaseAsync();
await hungGuardian.DetachAsync(hungLease);
await Task.Yield();
clock.Advance(TimeSpan.FromSeconds(5));
await hungGuardian.TickAsync();
Assert(hungServer.ForceTerminationRequests == 1,
    "A hung graceful shutdown cannot block the five-second forced-termination fallback.");
hungShutdown.SetResult();

var leaseGuardian = new AppServerGuardian(new TestGuardianAppServer(), clock);
await leaseGuardian.AcquireLeaseAsync();
clock.Advance(TimeSpan.FromSeconds(45));
await leaseGuardian.TickAsync();
Assert(leaseGuardian.IsFrontEndAbsent, "A missing 15-second heartbeat expires the Front End Lease after 45 seconds.");

var reattachServer = new TestGuardianAppServer();
var reattachGuardian = new AppServerGuardian(reattachServer, clock);
var originalLease = await reattachGuardian.AcquireLeaseAsync();
await reattachGuardian.RemoteTuiConnectedAsync();
await reattachGuardian.DetachAsync(originalLease);
var replacementLease = await reattachGuardian.AcquireLeaseAsync();
Assert(replacementLease.LeaseId != originalLease.LeaseId && reattachServer.GracefulShutdownRequests == 0,
    "Aura reattachment gets a fresh Front End Lease without replacing a live App Server.");

var slowServer = new TestGuardianAppServer { GracefulShutdown = new TaskCompletionSource() };
var uiSafeGuardian = new AppServerGuardian(slowServer, clock);
var session = new GuardianFrontEndSession(uiSafeGuardian);
await session.StartOrAttachAsync();
session.BeginDetach();
Assert(!slowServer.GracefulShutdown!.Task.IsCompleted, "Beginning UI exit does not wait for App Server shutdown.");
slowServer.GracefulShutdown.SetResult();
await session.DisposeAsync();

Console.WriteLine("PASS: Guardian lease, reattachment, drain, forced termination, and non-blocking exit tests.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class TestGuardianClock(DateTimeOffset initial) : IGuardianClock
{
    public DateTimeOffset UtcNow { get; private set; } = initial;
    public void Advance(TimeSpan by) => UtcNow += by;
}

sealed class TestGuardianAppServer : IGuardianAppServer
{
    public int GracefulShutdownRequests { get; private set; }
    public int ForceTerminationRequests { get; private set; }
    public bool HasExited => false;
    public TaskCompletionSource? GracefulShutdown { get; init; }

    public Task RequestGracefulShutdownAsync(CancellationToken cancellationToken = default)
    {
        GracefulShutdownRequests++;
        return GracefulShutdown?.Task ?? Task.CompletedTask;
    }

    public Task ForceTerminateAsync(CancellationToken cancellationToken = default)
    {
        ForceTerminationRequests++;
        return Task.CompletedTask;
    }
}
