namespace AgentAura.Prototype.Models;

public sealed class PrototypeViewModel : INotifyPropertyChanged
{
    private bool _isPinned;
    private bool _isHeaderVisible = true;
    private string _integrationStatus = "Windows observation-shell prototype";
    private readonly Dictionary<string, string> _threadAliases = new(StringComparer.Ordinal);
    private string? _aliasPath;

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
                "Verify managed App Server Thread subscriptions",
                "codex-integration",
                @"C:\code\codex-integration",
                "Comparing supported local transports."),
            new(
                AgentItemState.Completed,
                "Record the WPF shell feasibility gates",
                "agent-aura",
                @"C:\code\agent-aura",
                "Finished successfully 2 minutes ago.")
        };
    }

    public ObservableCollection<AgentItemSample> Items { get; }

    public string IntegrationStatus
    {
        get => _integrationStatus;
        set
        {
            if (_integrationStatus == value)
            {
                return;
            }

            _integrationStatus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntegrationStatus)));
        }
    }

    public void EnableBridgeMode(string endpoint, string aliasPath)
    {
        Items.Clear();
        _aliasPath = aliasPath;
        LoadAliases();
        IntegrationStatus = $"Bridge prototype · {endpoint}";
    }

    internal void ApplyBridgeSnapshot(IReadOnlyCollection<BridgePrototype.BridgeThreadState> snapshot)
    {
        Items.Clear();
        foreach (var thread in snapshot)
        {
            _threadAliases.TryGetValue(thread.ThreadId, out var alias);
            Items.Add(thread.ToAgentItem(alias));
        }
    }

    public bool ApplyPrototypeAlias(AgentItemSample item)
    {
        if (string.IsNullOrWhiteSpace(item.ThreadId) || string.IsNullOrWhiteSpace(_aliasPath))
        {
            return false;
        }

        var alias = $"Alias {item.ThreadId[..Math.Min(8, item.ThreadId.Length)]}";
        _threadAliases[item.ThreadId] = alias;
        item.Title = alias;
        Directory.CreateDirectory(Path.GetDirectoryName(_aliasPath)!);
        File.WriteAllText(_aliasPath, JsonSerializer.Serialize(_threadAliases, new JsonSerializerOptions { WriteIndented = true }));
        return true;
    }

    private void LoadAliases()
    {
        _threadAliases.Clear();
        if (string.IsNullOrWhiteSpace(_aliasPath) || !File.Exists(_aliasPath))
        {
            return;
        }

        var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_aliasPath));
        if (aliases is null)
        {
            return;
        }

        foreach (var (threadId, alias) in aliases)
        {
            if (!string.IsNullOrWhiteSpace(threadId) && !string.IsNullOrWhiteSpace(alias))
            {
                _threadAliases[threadId] = alias;
            }
        }
    }

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
