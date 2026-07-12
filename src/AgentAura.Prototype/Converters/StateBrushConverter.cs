namespace AgentAura.Prototype.Converters;

public sealed class StateBrushConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Models.AgentItemState state
            ? Models.AgentItemStatePresentations.For(state).Brush
            : System.Windows.Media.Brushes.SlateGray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
