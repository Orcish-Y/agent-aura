namespace AgentAura.Prototype.Models;

public enum AgentItemState
{
    Observed,
    Running,
    Attention,
    Completed,
    Failed,
    Interrupted,
    Disconnected
}

public sealed record AgentItemStatePresentation(System.Windows.Media.Brush Brush, string Glyph);

public static class AgentItemStatePresentations
{
    public static AgentItemStatePresentation For(AgentItemState state) => state switch
    {
        AgentItemState.Observed => Create(112, 119, 128, "○"),
        AgentItemState.Attention => Create(183, 143, 61, "!"),
        AgentItemState.Running => Create(74, 121, 166, "▶"),
        AgentItemState.Completed => Create(86, 139, 106, "✓"),
        AgentItemState.Failed => Create(168, 87, 87, "×"),
        AgentItemState.Interrupted => Create(126, 102, 149, "■"),
        _ => Create(112, 119, 128, "⌁")
    };

    private static AgentItemStatePresentation Create(byte red, byte green, byte blue, string glyph) =>
        new(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(red, green, blue)), glyph);
}
