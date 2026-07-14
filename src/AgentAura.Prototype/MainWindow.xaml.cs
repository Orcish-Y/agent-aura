using AgentAura.Prototype.BridgePrototype;
using AgentAura.Prototype.Models;

namespace AgentAura.Prototype;

public partial class MainWindow : Window
{
    private readonly WindowStateStore _windowStateStore;
    private readonly TrayController _trayController;
    private readonly PrototypeViewModel _viewModel = new();
    private readonly BridgePrototypeController? _bridgePrototype;
    private bool _canClose;

    public MainWindow(WindowStateStore windowStateStore, TrayController trayController)
    {
        _windowStateStore = windowStateStore;
        _trayController = trayController;
        _bridgePrototype = BridgePrototypeController.TryCreate(_viewModel);
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = _viewModel.IsPinned;
    }

    public void TogglePinning()
    {
        _viewModel.IsPinned = !_viewModel.IsPinned;
        Topmost = _viewModel.IsPinned;
        _viewModel.IsHeaderVisible = !_viewModel.IsPinned || IsMouseOver;
    }

    public void SaveState() => _windowStateStore.Save(new PrototypeWindowState(Left, Top, Width, Height, _viewModel.IsPinned));

    public void CloseForExit()
    {
        if (_bridgePrototype is not null)
        {
            _bridgePrototype.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _canClose = true;
        Close();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var state = _windowStateStore.RecoverToVisibleScreen(_windowStateStore.LoadOrDefault());
        Left = state.Left;
        Top = state.Top;
        Width = state.Width;
        Height = state.Height;
        _viewModel.IsPinned = state.IsPinned;
        Topmost = state.IsPinned;
        _viewModel.IsHeaderVisible = !state.IsPinned || IsMouseOver;
        if (_bridgePrototype is not null)
        {
            await _bridgePrototype.StartAsync();
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_canClose)
        {
            return;
        }

        e.Cancel = true;
        SaveState();
        Hide();
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnWindowMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsPinned)
        {
            _viewModel.IsHeaderVisible = true;
        }
    }

    private void OnWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsPinned)
        {
            _viewModel.IsHeaderVisible = false;
        }
    }

    private void OnPinClicked(object sender, RoutedEventArgs e) => TogglePinning();

    private void OnHideClicked(object sender, RoutedEventArgs e)
    {
        SaveState();
        Hide();
    }

    private void OnMinimiseClicked(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            SaveState();
            Hide();
        }
    }

    private void OnFlashTrayClicked(object sender, RoutedEventArgs e) => _trayController.StartFlashing();

    private void OnAgentItemMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is AgentItemSample item)
        {
            item.IsHovered = true;
        }
    }

    private void OnAgentItemMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is AgentItemSample item)
        {
            item.IsHovered = false;
        }
    }

    private void OnEditClicked(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is AgentItemSample item)
        {
            if (!_viewModel.ApplyPrototypeAlias(item))
            {
                item.Title = $"Alias: {item.Title}";
            }
        }
    }

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is AgentItemSample item)
        {
            _viewModel.Items.Remove(item);
        }
    }
}
