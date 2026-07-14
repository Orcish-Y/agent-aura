using AgentAura.Prototype.Models;

namespace AgentAura.Prototype.BridgePrototype;

// PROTOTYPE: orchestration seam enabled only by AGENT_AURA_BRIDGE_PROTOTYPE=1.
internal sealed class BridgePrototypeController : IAsyncDisposable
{
    private readonly PrototypeViewModel _viewModel;
    private readonly CodexAppServerHost? _host;
    private readonly CodexAppServerObserver _observer;
    private readonly CancellationTokenSource _stopping = new();

    private BridgePrototypeController(
        PrototypeViewModel viewModel,
        CodexAppServerHost? host,
        CodexAppServerObserver observer)
    {
        _viewModel = viewModel;
        _host = host;
        _observer = observer;
        _observer.StatusChanged += OnStatusChanged;
        _observer.SnapshotChanged += OnSnapshotChanged;
    }

    public static BridgePrototypeController? TryCreate(PrototypeViewModel viewModel)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("AGENT_AURA_BRIDGE_PROTOTYPE"), "1", StringComparison.Ordinal))
        {
            return null;
        }

        var endpoint = new Uri(Environment.GetEnvironmentVariable("AGENT_AURA_CODEX_ENDPOINT") ?? "ws://127.0.0.1:4500");
        var evidenceDirectory = Environment.GetEnvironmentVariable("AGENT_AURA_BRIDGE_EVIDENCE")
            ?? throw new InvalidOperationException("AGENT_AURA_BRIDGE_EVIDENCE is required in bridge prototype mode.");
        var attachOnly = string.Equals(
            Environment.GetEnvironmentVariable("AGENT_AURA_BRIDGE_ATTACH_ONLY"),
            "1",
            StringComparison.Ordinal);
        var host = attachOnly
            ? null
            : new CodexAppServerHost(
                endpoint,
                Environment.GetEnvironmentVariable("AGENT_AURA_CODEX_HOME")
                    ?? throw new InvalidOperationException("AGENT_AURA_CODEX_HOME is required when the bridge prototype owns the App Server."),
                Path.Combine(evidenceDirectory, "app-server.log"));

        viewModel.EnableBridgeMode(endpoint.ToString(), Path.Combine(evidenceDirectory, "aliases.json"));
        return new BridgePrototypeController(
            viewModel,
            host,
            new CodexAppServerObserver(endpoint, Path.Combine(evidenceDirectory, "observer.jsonl")));
    }

    public async Task StartAsync()
    {
        try
        {
            if (_host is not null)
            {
                _viewModel.IntegrationStatus = "Starting owned Codex App Server …";
                await _host.StartAsync(_stopping.Token);
                _viewModel.IntegrationStatus = "App Server ready; starting independent observer …";
            }
            else
            {
                _viewModel.IntegrationStatus = "Attaching observer to existing App Server …";
            }

            await _observer.StartAsync(_stopping.Token);
        }
        catch (Exception exception)
        {
            _viewModel.IntegrationStatus = $"Bridge prototype failed: {exception.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stopping.Cancel();
        await _observer.DisposeAsync();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
        _stopping.Dispose();
    }

    private void OnStatusChanged(string status) =>
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() => _viewModel.IntegrationStatus = status);

    private void OnSnapshotChanged(IReadOnlyCollection<BridgeThreadState> snapshot) =>
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() => _viewModel.ApplyBridgeSnapshot(snapshot));
}
