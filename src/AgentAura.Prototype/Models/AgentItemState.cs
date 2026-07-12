namespace AgentAura.Prototype.Models;

public enum AgentItemState
{
    Running,
    Attention,
    Succeeded,
    Failed,
    Interrupted,
    Unknown
}

public sealed record AgentItemStatePresentation(System.Windows.Media.Brush Brush, string Glyph);

public static class AgentItemStatePresentations
{
    public static AgentItemStatePresentation For(AgentItemState state) => state switch
    {
        AgentItemState.Attention => Create(183, 143, 61, "!"),
        AgentItemState.Running => Create(74, 121, 166, "▶"),
        AgentItemState.Succeeded => Create(86, 139, 106, "✓"),
        AgentItemState.Failed => Create(168, 87, 87, "×"),
        AgentItemState.Interrupted => Create(126, 102, 149, "■"),
        _ => Create(112, 119, 128, "?")
    };

    private static AgentItemStatePresentation Create(byte red, byte green, byte blue, string glyph) =>
        new(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(red, green, blue)), glyph);
}
