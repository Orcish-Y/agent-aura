namespace AgentAura.Prototype.Controls;

public sealed class ScrollingTextTimeline
{
    public double OverflowWidth { get; }

    public TimeSpan InitialDelay { get; }

    public TimeSpan EndPause { get; }

    public TimeSpan TravelDuration { get; }

    public TimeSpan CycleDuration { get; }

    public ScrollingTextTimeline(
        double overflowWidth,
        TimeSpan initialDelay,
        TimeSpan endPause,
        double pixelsPerSecond)
    {
        if (overflowWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overflowWidth));
        }

        if (initialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay));
        }

        if (endPause < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(endPause));
        }

        if (pixelsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerSecond));
        }

        OverflowWidth = overflowWidth;
        InitialDelay = initialDelay;
        EndPause = endPause;
        TravelDuration = TimeSpan.FromSeconds(overflowWidth / pixelsPerSecond);
        CycleDuration = initialDelay + TravelDuration + endPause + TravelDuration + endPause;
    }

    public double OffsetAt(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        var position = TimeSpan.FromTicks(elapsed.Ticks % CycleDuration.Ticks);
        if (position <= InitialDelay)
        {
            return 0;
        }

        position -= InitialDelay;
        if (position <= TravelDuration)
        {
            return -OverflowWidth * position.Ticks / TravelDuration.Ticks;
        }

        position -= TravelDuration;
        if (position <= EndPause)
        {
            return -OverflowWidth;
        }

        position -= EndPause;
        if (position <= TravelDuration)
        {
            return -OverflowWidth + (OverflowWidth * position.Ticks / TravelDuration.Ticks);
        }

        return 0;
    }
}
