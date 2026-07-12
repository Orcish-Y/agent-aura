using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace AgentAura.Prototype;

public sealed class TrayController : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.Timer _flashTimer = new() { Interval = 500 };
    private bool _isFlashing;
    private bool _showAlertIcon;

    public TrayController(Action showWindow, Action hideWindow, Action togglePinning, Action exitApplication)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => showWindow());
        menu.Items.Add("Hide", null, (_, _) => hideWindow());
        menu.Items.Add("Pin / unpin", null, (_, _) => togglePinning());
        menu.Items.Add("Settings", null, (_, _) => showWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exitApplication());

        _icon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Information,
            Text = "Agent Aura prototype",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == Forms.MouseButtons.Left)
            {
                showWindow();
            }
        };
        _flashTimer.Tick += (_, _) => AdvanceFlashFrame();
    }

    public void StartFlashing()
    {
        if (_isFlashing)
        {
            return;
        }

        _isFlashing = true;
        _flashTimer.Start();
    }

    public void StopFlashing()
    {
        _isFlashing = false;
        _showAlertIcon = false;
        _flashTimer.Stop();
        _icon.Icon = Drawing.SystemIcons.Information;
    }

    private void AdvanceFlashFrame()
    {
        _showAlertIcon = !_showAlertIcon;
        _icon.Icon = _showAlertIcon ? Drawing.SystemIcons.Warning : Drawing.SystemIcons.Information;
    }

    public void Dispose()
    {
        _flashTimer.Dispose();
        _icon.Dispose();
    }
}
