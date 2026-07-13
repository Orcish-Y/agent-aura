using AgentAura.Prototype.Controls;

internal static class Program
{
    private static int Main()
    {
        try
        {
            AssertOverflowPresentation();

            var timeline = new ScrollingTextTimeline(
                overflowWidth: 24,
                initialDelay: TimeSpan.FromMilliseconds(500),
                endPause: TimeSpan.FromMilliseconds(500),
                pixelsPerSecond: 48);

            AssertOffset(timeline, 0, 0, "the beginning of the initial delay");
            AssertOffset(timeline, 250, 0, "the middle of the initial delay");
            AssertOffset(timeline, 750, -12, "the middle of the outward scroll");
            AssertOffset(timeline, 1_000, -24, "the far end");
            AssertOffset(timeline, 1_250, -24, "the middle of the far-end pause");
            AssertOffset(timeline, 1_750, -12, "the middle of the return scroll");
            AssertOffset(timeline, 2_000, 0, "the near end");
            AssertOffset(timeline, 2_250, 0, "the middle of the near-end pause");
            AssertOffset(timeline, 3_250, -12, "the next outward scroll");

            Console.WriteLine("PASS: The overflow-scroll timeline delays, pauses at both ends, and repeats.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            return 1;
        }
    }

    private static void AssertOverflowPresentation()
    {
        AssertDisplay(
            isPointerOver: false,
            hasOverflow: true,
            expected: ScrollingTextDisplay.TruncatedWithEllipsis,
            "an overflowing line without direct hover");
        AssertDisplay(
            isPointerOver: true,
            hasOverflow: true,
            expected: ScrollingTextDisplay.Scrolling,
            "the directly hovered overflowing line");
        AssertDisplay(
            isPointerOver: true,
            hasOverflow: false,
            expected: ScrollingTextDisplay.FullText,
            "a directly hovered line that fits");
        AssertDisplay(
            isPointerOver: false,
            hasOverflow: false,
            expected: ScrollingTextDisplay.FullText,
            "a line that fits without hover");

        var hoveredOverflowVisual = ScrollingTextPresentation.ResolveVisualState(
            isPointerOver: true,
            hasOverflow: true);
        if (hoveredOverflowVisual is not
            { ShowsTruncatedText: false, ShowsFullText: true, ShouldScroll: true })
        {
            throw new InvalidOperationException(
                "A hovered overflowing line does not replace the truncated text with the full scrolling text layer.");
        }

        if (!hoveredOverflowVisual.KeepsTruncatedLayout)
        {
            throw new InvalidOperationException(
                "A hovered overflowing fourth Agent Item line does not preserve the truncated layer's layout footprint.");
        }
    }

    private static void AssertDisplay(
        bool isPointerOver,
        bool hasOverflow,
        ScrollingTextDisplay expected,
        string scenario)
    {
        var actual = ScrollingTextPresentation.Resolve(isPointerOver, hasOverflow);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Expected {expected} for {scenario}, but received {actual}.");
        }
    }

    private static void AssertOffset(
        ScrollingTextTimeline timeline,
        double elapsedMilliseconds,
        double expectedOffset,
        string moment)
    {
        var actualOffset = timeline.OffsetAt(TimeSpan.FromMilliseconds(elapsedMilliseconds));
        if (Math.Abs(actualOffset - expectedOffset) > 0.01)
        {
            throw new InvalidOperationException(
                $"Expected offset {expectedOffset} at {moment}, but received {actualOffset}.");
        }
    }
}
