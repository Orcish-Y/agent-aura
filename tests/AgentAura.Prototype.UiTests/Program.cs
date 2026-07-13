using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
                Width = 700,
                Height = 28,
                Text = "This deliberately long Agent Item detail must overflow the observation window."
            };

            window = new Window
            {
                Width = 720,
                Height = 50,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Content = scrollingText
            };

            window.Show();
            window.UpdateLayout();

            var truncatedTextBlock = FindDescendant<TextBlock>(
                scrollingText,
                textBlock => textBlock.TextTrimming == TextTrimming.CharacterEllipsis)
                ?? throw new InvalidOperationException("The ScrollingText truncated TextBlock was not created.");
            var scrollingTextBlock = FindDescendant<TextBlock>(
                scrollingText,
                textBlock => textBlock.RenderTransform is TranslateTransform)
                ?? throw new InvalidOperationException("The ScrollingText full-text TextBlock was not created.");

            AssertOverflowScrollsAfterDelayAndPausesAtBothEnds(
                window,
                scrollingText,
                truncatedTextBlock,
                scrollingTextBlock);
            AssertHoveredOverflowDoesNotCollapseItsRow();
            AssertReducedMotionHasBeenRemoved(application);
            AssertWindowPinStatePreservesAgentMessageItemPositions(application);

            Console.WriteLine("PASS: Agent Item text, Window Pin State, and motion preferences match the prototype behaviour.");
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

    private static void AssertOverflowScrollsAfterDelayAndPausesAtBothEnds(
        Window window,
        ScrollingText control,
        TextBlock truncatedTextBlock,
        TextBlock scrollingTextBlock)
    {
        scrollingTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var naturalTextWidth = scrollingTextBlock.DesiredSize.Width;
        control.Width = naturalTextWidth - 24;
        window.UpdateLayout();

        var overflow = naturalTextWidth - control.ActualWidth;
        if (overflow <= 0)
        {
            throw new InvalidOperationException(
                "The test fixture does not overflow the ScrollingText viewport.");
        }

        if (scrollingTextBlock.RenderTransform is not TranslateTransform textTransform)
        {
            throw new InvalidOperationException(
                "Overflowing text has no horizontal transform to drive its internal scroll.");
        }

        PumpFor(TimeSpan.FromMilliseconds(20));
        if (!truncatedTextBlock.IsVisible || scrollingTextBlock.IsVisible)
        {
            throw new InvalidOperationException(
                "Overflowing text does not display only its truncated layer while its own row is not hovered.");
        }

        RaisePointerEvent(control, "OnMouseEnter");
        PumpFor(TimeSpan.FromMilliseconds(20));
        if (truncatedTextBlock.IsVisible || !scrollingTextBlock.IsVisible)
        {
            throw new InvalidOperationException(
                "Overflowing text does not replace its truncated layer with its full-text layer while its own row is hovered.");
        }

        if (scrollingTextBlock.ActualWidth < naturalTextWidth - 0.5)
        {
            throw new InvalidOperationException(
                "Hovered overflowing text is still arranged to the truncated viewport width.");
        }

        PumpFor(TimeSpan.FromMilliseconds(250));
        if (Math.Abs(textTransform.X) > 0.5)
        {
            throw new InvalidOperationException("Overflowing text moved before its initial delay elapsed.");
        }

        WaitUntil(
            () => textTransform.X < -1,
            TimeSpan.FromSeconds(2),
            "Overflowing text did not begin scrolling after its initial delay.");

        WaitUntil(
            () => textTransform.X <= -overflow + 0.75,
            TimeSpan.FromSeconds(2),
            "Overflowing text did not reach the far end of its viewport.");

        var farEndPosition = textTransform.X;
        PumpFor(TimeSpan.FromMilliseconds(250));
        if (Math.Abs(textTransform.X - farEndPosition) > 0.5)
        {
            throw new InvalidOperationException("Overflowing text did not pause at the far end.");
        }

        WaitUntil(
            () => textTransform.X > farEndPosition + 1,
            TimeSpan.FromSeconds(2),
            "Overflowing text did not scroll back from the far end.");

        RaisePointerEvent(control, "OnMouseLeave");
        PumpFor(TimeSpan.FromMilliseconds(20));
        if (!truncatedTextBlock.IsVisible || scrollingTextBlock.IsVisible || Math.Abs(textTransform.X) > 0.5)
        {
            throw new InvalidOperationException(
                "Overflowing text did not return to its truncated layer when its own row was no longer hovered.");
        }
    }

    private static void AssertHoveredOverflowDoesNotCollapseItsRow()
    {
        var scrollingText = new ScrollingText
        {
            Width = 240,
            Text = "This deliberately long fourth Agent Item line must keep its row height while scrolling."
        };
        var layoutRoot = new Grid();
        layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layoutRoot.Children.Add(scrollingText);

        var window = new Window
        {
            Width = 260,
            Height = 100,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = layoutRoot
        };

        try
        {
            window.Show();
            window.UpdateLayout();
            var collapsedHeight = scrollingText.ActualHeight;
            if (collapsedHeight <= 0)
            {
                throw new InvalidOperationException("The fourth Agent Item line was not measured while truncated.");
            }

            RaisePointerEvent(scrollingText, "OnMouseEnter");
            PumpFor(TimeSpan.FromMilliseconds(20));
            window.UpdateLayout();

            if (scrollingText.ActualHeight < collapsedHeight - 0.5)
            {
                throw new InvalidOperationException(
                    "Hovering an overflowing fourth Agent Item line collapses its row and loses the item's hover target.");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void RaisePointerEvent(ScrollingText control, string handlerName)
    {
        var handler = typeof(ScrollingText).GetMethod(
            handlerName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The ScrollingText {handlerName} handler was not found.");

        handler.Invoke(control, [control, null]);
    }

    private static void WaitUntil(Func<bool> predicate, TimeSpan timeout, string failureMessage)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (predicate())
            {
                return;
            }

            PumpFor(TimeSpan.FromMilliseconds(20));
        }

        throw new InvalidOperationException(failureMessage);
    }

    private static void PumpFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = duration
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
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

    private static void AssertWindowPinStatePreservesAgentMessageItemPositions(Application application)
    {
        application.Resources["BooleanToVisibilityConverter"] = new BooleanToVisibilityConverter();
        using var trayController = new TrayController(() => { }, () => { }, () => { }, () => { });
        var mainWindow = new MainWindow(new WindowStateStore(), trayController);

        try
        {
            mainWindow.Show();
            mainWindow.UpdateLayout();

            var pinButton = FindDescendant<Button>(
                mainWindow,
                button => button.Content is string content && content == "Pin")
                ?? throw new InvalidOperationException("The Window Pin State control was not rendered.");
            var headerTitle = FindDescendant<TextBlock>(
                mainWindow,
                textBlock => textBlock.Text == "Agent Aura")
                ?? throw new InvalidOperationException("The observation window Header was not rendered.");
            var header = VisualTreeHelper.GetParent(
                VisualTreeHelper.GetParent(headerTitle)) as Grid
                ?? throw new InvalidOperationException("The observation window Header was not hosted in its public visual container.");
            var agentMessageItem = FindDescendant<ListBoxItem>(mainWindow)
                ?? throw new InvalidOperationException("No Agent Message Item was rendered in the observation window.");

            if (mainWindow.Topmost)
            {
                pinButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                mainWindow.UpdateLayout();
            }

            pinButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            RaiseMouseEvent(mainWindow, Mouse.MouseEnterEvent);
            mainWindow.UpdateLayout();

            if (!mainWindow.Topmost || header.Opacity < 0.99)
            {
                throw new InvalidOperationException(
                    "Enabling Window Pin State does not keep the window topmost with its Header visible while the pointer is inside.");
            }

            var itemTopWhileHeaderVisible = GetTopWithinWindow(agentMessageItem, mainWindow);
            var headerFootprint = header.ActualHeight;

            RaiseMouseEvent(mainWindow, Mouse.MouseLeaveEvent);
            mainWindow.UpdateLayout();

            AssertNearlyEqual(
                itemTopWhileHeaderVisible,
                GetTopWithinWindow(agentMessageItem, mainWindow),
                "Hiding the pinned Header moved an Agent Message Item.");
            if (header.Opacity > 0.01 || header.ActualHeight < headerFootprint - 0.5)
            {
                throw new InvalidOperationException(
                    "The pinned Header does not hide while preserving its layout footprint.");
            }

            RaiseMouseEvent(mainWindow, Mouse.MouseEnterEvent);
            mainWindow.UpdateLayout();

            AssertNearlyEqual(
                itemTopWhileHeaderVisible,
                GetTopWithinWindow(agentMessageItem, mainWindow),
                "Revealing the pinned Header moved an Agent Message Item.");
            if (header.Opacity < 0.99)
            {
                throw new InvalidOperationException("Entering the pinned observation window did not reveal the Header.");
            }

            pinButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            RaiseMouseEvent(mainWindow, Mouse.MouseLeaveEvent);
            mainWindow.UpdateLayout();

            if (mainWindow.Topmost || header.Opacity < 0.99)
            {
                throw new InvalidOperationException(
                    "Disabling Window Pin State did not restore normal stacking with a visible Header.");
            }
        }
        finally
        {
            mainWindow.CloseForExit();
        }
    }

    private static void RaiseMouseEvent(UIElement target, RoutedEvent routedEvent)
    {
        target.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        {
            RoutedEvent = routedEvent
        });
    }

    private static double GetTopWithinWindow(FrameworkElement element, Window window) =>
        element.TransformToAncestor(window).Transform(new Point()).Y;

    private static void AssertNearlyEqual(double expected, double actual, string failureMessage)
    {
        if (Math.Abs(expected - actual) > 0.5)
        {
            throw new InvalidOperationException($"{failureMessage} Expected {expected:F1}, received {actual:F1}.");
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
