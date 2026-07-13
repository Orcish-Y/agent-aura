namespace AgentAura.Prototype.Controls;

public partial class ScrollingText : System.Windows.Controls.UserControl
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EndPause = TimeSpan.FromMilliseconds(500);
    private const double PixelsPerSecond = 48;

    private bool _isLoaded;
    private bool _isPointerOver;
    private bool _scrollUpdatePending;

    public ScrollingText()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ScrollingText),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        ((ScrollingText)dependencyObject).QueueScrollUpdate();
    }

    private void OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        _isLoaded = true;
        QueueScrollUpdate();
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs eventArgs)
    {
        _isPointerOver = true;
        QueueScrollUpdate();
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs eventArgs)
    {
        _isPointerOver = false;
        QueueScrollUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        _isLoaded = false;
        _isPointerOver = false;
        StopScrolling();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs eventArgs) => QueueScrollUpdate();

    private void QueueScrollUpdate()
    {
        if (!_isLoaded || _scrollUpdatePending)
        {
            return;
        }

        _scrollUpdatePending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _scrollUpdatePending = false;
            UpdateScrolling();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateScrolling()
    {
        StopScrolling();
        TruncatedTextContent.Visibility = Visibility.Visible;
        ScrollingViewport.Visibility = Visibility.Collapsed;

        if (!_isLoaded || ActualWidth <= 0)
        {
            return;
        }

        ScrollingTextContent.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var overflowWidth = ScrollingTextContent.DesiredSize.Width - ActualWidth;
        var visualState = ScrollingTextPresentation.ResolveVisualState(_isPointerOver, overflowWidth > 0);
        TruncatedTextContent.Visibility = visualState.ShowsTruncatedText
            ? Visibility.Visible
            : Visibility.Collapsed;
        ScrollingViewport.Visibility = visualState.ShowsFullText
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!visualState.ShouldScroll)
        {
            return;
        }

        var timeline = new ScrollingTextTimeline(
            overflowWidth,
            InitialDelay,
            EndPause,
            PixelsPerSecond);
        var animation = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames
        {
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        var keyTime = TimeSpan.Zero;

        animation.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0, keyTime));
        keyTime += timeline.InitialDelay;
        animation.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0, keyTime));
        keyTime += timeline.TravelDuration;
        animation.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(-timeline.OverflowWidth, keyTime));
        keyTime += timeline.EndPause;
        animation.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(-timeline.OverflowWidth, keyTime));
        keyTime += timeline.TravelDuration;
        animation.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0, keyTime));
        keyTime += timeline.EndPause;
        animation.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0, keyTime));

        TextTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
    }

    private void StopScrolling()
    {
        TextTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
        TextTransform.X = 0;
    }

}
