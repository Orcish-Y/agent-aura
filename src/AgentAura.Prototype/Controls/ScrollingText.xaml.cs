namespace AgentAura.Prototype.Controls;

public partial class ScrollingText : System.Windows.Controls.UserControl
{
    public ScrollingText()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ScrollingText),
        new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

}
