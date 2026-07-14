namespace AgentAura.Core.Items;

public sealed record AttentionPinSpan
{
    private AttentionPinSpan(int? significantUpdates)
    {
        SignificantUpdates = significantUpdates;
    }

    public int? SignificantUpdates { get; }
    public bool IsAlwaysPinned => SignificantUpdates is null;

    public static AttentionPinSpan AlwaysPinned { get; } = new((int?)null);

    public static AttentionPinSpan ForUpdates(int significantUpdates) =>
        significantUpdates is >= 1 and <= 50
            ? new AttentionPinSpan(significantUpdates)
            : throw new ArgumentOutOfRangeException(nameof(significantUpdates), "Attention Pin Span must be from 1 to 50.");
}
