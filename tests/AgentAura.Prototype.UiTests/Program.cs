using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgentAura.Prototype;
using AgentAura.Prototype.Controls;
using AgentAura.Prototype.Models;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Window? window = null;

        try
        {
            var scrollingText = new ScrollingText
            {
                Width = 120,
                Height = 28,
                Text = "This deliberately long Agent Item detail must overflow the observation window."
            };

            window = new Window
            {
                Width = 140,
                Height = 50,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Content = scrollingText
            };

            window.Show();
            window.UpdateLayout();

            var textBlock = FindDescendant<TextBlock>(scrollingText)
                ?? throw new InvalidOperationException("The ScrollingText content TextBlock was not created.");

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (textBlock.DesiredSize.Width <= scrollingText.ActualWidth)
            {
                throw new InvalidOperationException("The test fixture does not overflow the ScrollingText viewport.");
            }

            AssertOverflowIsEllipsized(window, scrollingText, textBlock, width: 119);
            AssertReducedMotionHasBeenRemoved(application);

            Console.WriteLine("PASS: Overflowing Agent Item text is ellipsized and Reduced motion is absent from the prototype.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            return 1;
        }
        finally
        {
            window?.Close();
            application.Shutdown();
        }
    }

    private static void AssertOverflowIsEllipsized(
        Window window,
        ScrollingText control,
        TextBlock textBlock,
        double width)
    {
        control.Width = width;
        window.UpdateLayout();

        if (textBlock.TextTrimming != TextTrimming.CharacterEllipsis)
        {
            throw new InvalidOperationException(
                $"Overflowing text uses {textBlock.TextTrimming}; expected CharacterEllipsis.");
        }
    }

    private static void AssertReducedMotionHasBeenRemoved(Application application)
    {
        if (typeof(PrototypeViewModel).GetProperty("ReducedMotion") is not null ||
            typeof(ScrollingText).GetProperty("IsMotionEnabled") is not null)
        {
            throw new InvalidOperationException("Reduced motion state is still exposed by the prototype.");
        }

        application.Resources["BooleanToVisibilityConverter"] = new BooleanToVisibilityConverter();
        using var trayController = new TrayController(() => { }, () => { }, () => { }, () => { });
        var mainWindow = new MainWindow(new WindowStateStore(), trayController);

        try
        {
            mainWindow.Show();
            mainWindow.UpdateLayout();

            if (FindDescendant<CheckBox>(
                    mainWindow,
                    checkBox => checkBox.Content is string content && content == "Reduced motion") is not null)
            {
                throw new InvalidOperationException("The main window still displays a Reduced motion checkbox.");
            }
        }
        finally
        {
            mainWindow.CloseForExit();
        }
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool>? predicate = null)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match && (predicate is null || predicate(match)))
            {
                return match;
            }

            var descendant = FindDescendant(child, predicate);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
