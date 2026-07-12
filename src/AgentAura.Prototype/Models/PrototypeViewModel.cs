namespace AgentAura.Prototype.Models;

public sealed class PrototypeViewModel : INotifyPropertyChanged
{
    private bool _isPinned;
    private bool _isHeaderVisible = true;

    public PrototypeViewModel()
    {
        Items = new ObservableCollection<AgentItemSample>
        {
            new(
                AgentItemState.Attention,
                "Prepare a Windows self-contained installer and explain the ownership boundaries",
                "agent-aura",
                @"C:\code\agent-aura",
                "Waiting for your approval to install the preview package."),
            new(
                AgentItemState.Running,
                "Map passive Codex Thread integration options",
                "codex-integration",
                @"C:\code\codex-integration",
                "Comparing supported local transports."),
            new(
                AgentItemState.Succeeded,
                "Record the WPF shell feasibility gates",
                "agent-aura",
                @"C:\code\agent-aura",
                "Finished successfully 2 minutes ago.")
        };
    }

    public ObservableCollection<AgentItemSample> Items { get; }

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value)
            {
                return;
            }

            _isPinned = value;
            IsHeaderVisible = !value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
        }
    }

    public bool IsHeaderVisible
    {
        get => _isHeaderVisible;
        set
        {
            if (_isHeaderVisible == value)
            {
                return;
            }

            _isHeaderVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHeaderVisible)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
