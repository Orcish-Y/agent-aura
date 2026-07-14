namespace AgentAura.Core.Guardian;

public sealed record GuardianEndpoint(string Endpoint, string? ControlToken);
public sealed record FrontEndLease(string LeaseId, DateTimeOffset ExpiresAt);
public sealed record GuardianHealth(bool IsAvailable, int RemoteTuiCount, string? Diagnostic);
public sealed record GuardianOperationResult(bool Succeeded, string? Diagnostic);

public interface IGuardianClient
{
    Task<GuardianHealth> StartOrAttachAsync(CancellationToken cancellationToken = default);
    Task<FrontEndLease> AcquireLeaseAsync(CancellationToken cancellationToken = default);
    Task RenewLeaseAsync(FrontEndLease lease, CancellationToken cancellationToken = default);
    Task DetachAsync(FrontEndLease lease, CancellationToken cancellationToken = default);
    Task<GuardianHealth> GetHealthAsync(CancellationToken cancellationToken = default);
    Task<GuardianOperationResult> DrainAsync(CancellationToken cancellationToken = default);
    Task<GuardianOperationResult> ForceStopAsync(CancellationToken cancellationToken = default);
}
