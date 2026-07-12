namespace AgentAura.Prototype.Models;

public sealed class AgentItemSample : INotifyPropertyChanged
{
    private bool _isHovered;
    private string _title;

    public AgentItemSample(
        AgentItemState state,
        string title,
        string project,
        string directory,
        string detail)
    {
        State = state;
        _title = title;
        Project = project;
        Directory = directory;
        Detail = detail;
    }

    public AgentItemState State { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public string Project { get; }

    public string Directory { get; }

    public string ProjectContext => $"{Project} · {Directory}";

    public string Detail { get; }

    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value)
            {
                return;
            }

            _isHovered = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHovered)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
