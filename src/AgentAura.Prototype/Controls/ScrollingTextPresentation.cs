namespace AgentAura.Prototype.Controls;

public enum ScrollingTextDisplay
{
    FullText,
    TruncatedWithEllipsis,
    Scrolling
}

public readonly record struct ScrollingTextVisualState(
    bool ShowsTruncatedText,
    bool ShowsFullText,
    bool ShouldScroll,
    bool KeepsTruncatedLayout);

public static class ScrollingTextPresentation
{
    public static ScrollingTextVisualState ResolveVisualState(bool isPointerOver, bool hasOverflow) =>
        Resolve(isPointerOver, hasOverflow) switch
        {
            ScrollingTextDisplay.Scrolling => new(
                ShowsTruncatedText: false,
                ShowsFullText: true,
                ShouldScroll: true,
                KeepsTruncatedLayout: true),
            _ => new(
                ShowsTruncatedText: true,
                ShowsFullText: false,
                ShouldScroll: false,
                KeepsTruncatedLayout: false)
        };

    public static ScrollingTextDisplay Resolve(bool isPointerOver, bool hasOverflow)
    {
        if (!hasOverflow)
        {
            return ScrollingTextDisplay.FullText;
        }

        return isPointerOver
            ? ScrollingTextDisplay.Scrolling
            : ScrollingTextDisplay.TruncatedWithEllipsis;
    }
}
