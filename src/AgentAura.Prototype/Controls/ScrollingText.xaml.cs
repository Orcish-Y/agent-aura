namespace AgentAura.Prototype.Controls;

public partial class ScrollingText : System.Windows.Controls.UserControl
{
    private readonly DispatcherTimer _hoverDelay = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private double _textWidth;

    public ScrollingText()
    {
        InitializeComponent();
        _hoverDelay.Tick += OnHoverDelayElapsed;
        Loaded += (_, _) => UpdateTextWidth();
        SizeChanged += (_, _) => UpdateTextWidth();
        MouseEnter += (_, _) => StartAfterHoverDelay();
        MouseLeave += (_, _) => StopScrolling();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ScrollingText),
        new PropertyMetadata(string.Empty, (control, _) => ((ScrollingText)control).UpdateTextWidth()));

    public static readonly DependencyProperty IsMotionEnabledProperty = DependencyProperty.Register(
        nameof(IsMotionEnabled),
        typeof(bool),
        typeof(ScrollingText),
        new PropertyMetadata(true, (control, _) => ((ScrollingText)control).ApplyMotionPreference()));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsMotionEnabled
    {
        get => (bool)GetValue(IsMotionEnabledProperty);
        set => SetValue(IsMotionEnabledProperty, value);
    }

    private void StartAfterHoverDelay()
    {
        if (IsMotionEnabled && _textWidth > ActualWidth)
        {
            _hoverDelay.Start();
        }
    }

    private void OnHoverDelayElapsed(object? sender, EventArgs e)
    {
        _hoverDelay.Stop();

        if (!IsMouseOver || !IsMotionEnabled || _textWidth <= ActualWidth)
        {
            return;
        }

        var overflow = _textWidth - ActualWidth;
        var pause = TimeSpan.FromMilliseconds(500);
        var travel = TimeSpan.FromMilliseconds(Math.Max(1_500, overflow * 25));
        var animation = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(pause)));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow, KeyTime.FromTimeSpan(pause + travel)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(-overflow, KeyTime.FromTimeSpan(pause + travel + pause)));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(pause + travel + pause + travel)));

        TextTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void StopScrolling()
    {
        _hoverDelay.Stop();
        TextTransform.BeginAnimation(TranslateTransform.XProperty, null);
        TextTransform.X = 0;
    }

    private void ApplyMotionPreference()
    {
        TextContent.TextTrimming = IsMotionEnabled ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        StopScrolling();
    }

    private void UpdateTextWidth()
    {
        if (!IsLoaded || string.IsNullOrEmpty(Text))
        {
            return;
        }

        var formatted = new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            new Typeface(TextContent.FontFamily, TextContent.FontStyle, TextContent.FontWeight, TextContent.FontStretch),
            TextContent.FontSize,
            TextContent.Foreground,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        _textWidth = formatted.WidthIncludingTrailingWhitespace;
        TextContent.TextTrimming = IsMotionEnabled ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        StopScrolling();
    }
}
