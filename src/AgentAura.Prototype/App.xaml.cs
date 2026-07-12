namespace AgentAura.Prototype;

public partial class App : System.Windows.Application
{
    private readonly TrayController _trayController;
    private readonly WindowStateStore _windowStateStore;
    private MainWindow? _mainWindow;

    public App()
    {
        _windowStateStore = new WindowStateStore();
        _trayController = new TrayController(ShowWindow, HideWindow, TogglePinning, ExitApplication);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow(_windowStateStore, _trayController);
        _mainWindow.Show();
    }

    private void ShowWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.ShowAndActivate();
        _trayController.StopFlashing();
    }

    private void HideWindow()
    {
        _mainWindow?.Hide();
    }

    private void TogglePinning()
    {
        _mainWindow?.TogglePinning();
    }

    private void ExitApplication()
    {
        _mainWindow?.SaveState();
        _mainWindow?.CloseForExit();
        _trayController.Dispose();
        Shutdown();
    }
}
