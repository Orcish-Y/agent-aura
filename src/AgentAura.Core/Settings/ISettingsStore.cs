namespace AgentAura.Core.Settings;

public interface ISettingsStore
{
    Task<DurableSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(DurableSettings settings, CancellationToken cancellationToken = default);
}
