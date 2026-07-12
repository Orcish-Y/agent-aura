using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace AgentAura.Prototype;

public sealed class WindowStateStore
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AgentAura",
        "Prototype",
        "window-state.json");

    public PrototypeWindowState LoadOrDefault()
    {
        try
        {
            return File.Exists(StatePath)
                ? JsonSerializer.Deserialize<PrototypeWindowState>(File.ReadAllText(StatePath)) ?? PrototypeWindowState.Default
                : PrototypeWindowState.Default;
        }
        catch (JsonException)
        {
            return PrototypeWindowState.Default;
        }
    }

    public void Save(PrototypeWindowState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
    }

    public PrototypeWindowState RecoverToVisibleScreen(PrototypeWindowState state)
    {
        var bounds = new Drawing.Rectangle((int)state.Left, (int)state.Top, (int)state.Width, (int)state.Height);
        var isVisible = Forms.Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
        if (isVisible)
        {
            return state;
        }

        var workArea = Forms.Screen.PrimaryScreen?.WorkingArea ?? new Drawing.Rectangle(0, 0, 1280, 720);
        return state with
        {
            Left = workArea.Left + Math.Max(0, (workArea.Width - state.Width) / 2),
            Top = workArea.Top + Math.Max(0, (workArea.Height - state.Height) / 2)
        };
    }
}

public sealed record PrototypeWindowState(double Left, double Top, double Width, double Height, bool IsPinned)
{
    public static PrototypeWindowState Default { get; } = new(80, 80, 520, 320, false);
}
