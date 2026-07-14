using AgentAura.Core.Items;

namespace AgentAura.Core.Settings;

public sealed record AuraSettings(
    int CollapsedCapacity,
    int ExpandedCapacity,
    int? AttentionPinSpan,
    bool WindowPinned,
    bool SignInLaunchEnabled,
    bool SilentStartup)
{
    public static AuraSettings Default { get; } = new(5, 15, 10, false, false, false);

    public AgentAura.Core.Items.AttentionPinSpan ToAttentionPinSpan() => AttentionPinSpan is null
        ? AgentAura.Core.Items.AttentionPinSpan.AlwaysPinned
        : AgentAura.Core.Items.AttentionPinSpan.ForUpdates(AttentionPinSpan.Value);

    public void Validate()
    {
        if (CollapsedCapacity is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(CollapsedCapacity));
        if (ExpandedCapacity is < 5 or > 30 || ExpandedCapacity < CollapsedCapacity) throw new ArgumentOutOfRangeException(nameof(ExpandedCapacity));
        if (AttentionPinSpan is not null) _ = AgentAura.Core.Items.AttentionPinSpan.ForUpdates(AttentionPinSpan.Value);
    }
}

public sealed record WslConnectionCredentials(string ControlToken, string? DefaultDistribution)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ControlToken) || ControlToken.Length < 32)
            throw new ArgumentException("The WSL Guardian control token must contain at least 32 characters.", nameof(ControlToken));
        if (DefaultDistribution is { Length: > 256 }) throw new ArgumentOutOfRangeException(nameof(DefaultDistribution));
    }
}

public sealed record DurableSettings(
    AuraSettings Settings,
    IReadOnlyDictionary<string, string> ThreadAliases,
    WslConnectionCredentials? WslConnection)
{
    public static DurableSettings Default { get; } = new(AuraSettings.Default, new Dictionary<string, string>(), null);

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Settings);
        Settings.Validate();
        ArgumentNullException.ThrowIfNull(ThreadAliases);
        foreach (var (threadId, alias) in ThreadAliases)
        {
            if (string.IsNullOrWhiteSpace(threadId) || string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Thread aliases require a non-empty stable Thread ID and display name.", nameof(ThreadAliases));
        }
        WslConnection?.Validate();
    }
}
