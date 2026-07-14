using System.Diagnostics;
using System.Net;

namespace AgentAura.Core.Observation;

public sealed record ManagedCodexAppServerEndpoint(Uri WebSocketEndpoint)
{
    public ManagedCodexAppServerEndpoint(string endpoint) : this(new Uri(endpoint)) { }

    public Uri ReadinessEndpoint => new UriBuilder(WebSocketEndpoint) { Scheme = "http", Path = "/readyz" }.Uri;
    public string RemoteTuiCommand => $"codex --remote {WebSocketEndpoint}";

    public void Validate()
    {
        if (WebSocketEndpoint.Scheme is not "ws" and not "wss") throw new ArgumentException("The managed App Server endpoint must use WebSocket.", nameof(WebSocketEndpoint));
        if (!IPAddress.TryParse(WebSocketEndpoint.Host, out var address) || !IPAddress.IsLoopback(address))
            throw new ArgumentException("The Windows MVP App Server must bind only to a loopback endpoint.", nameof(WebSocketEndpoint));
    }
}

/// <summary>Starts an Aura-owned local App Server only when no healthy managed listener already exists.</summary>
public sealed class ManagedCodexAppServer : IAsyncDisposable
{
    private static readonly TimeSpan ReadinessRequestTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReadinessRetryDelay = TimeSpan.FromMilliseconds(250);
    private const int ReadinessAttempts = 40;
    private readonly ManagedCodexAppServerEndpoint _endpoint;
    private readonly string _codexHome;
    private readonly HttpClient _httpClient;
    private Process? _process;

    public ManagedCodexAppServer(ManagedCodexAppServerEndpoint endpoint, string codexHome, HttpClient? httpClient = null)
    {
        endpoint.Validate();
        if (string.IsNullOrWhiteSpace(codexHome)) throw new ArgumentException("A private CODEX_HOME is required for the managed App Server.", nameof(codexHome));
        _endpoint = endpoint;
        _codexHome = codexHome;
        _httpClient = httpClient ?? new HttpClient { Timeout = ReadinessRequestTimeout };
    }

    public string RemoteTuiCommand => _endpoint.RemoteTuiCommand;
    public bool StartedByAura => _process is not null;

    public async Task StartOrAttachAsync(CancellationToken cancellationToken = default)
    {
        if (await IsReadyAsync(cancellationToken)) return;
        Directory.CreateDirectory(_codexHome);
        var start = new ProcessStartInfo("codex") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("app-server");
        start.ArgumentList.Add("--listen");
        start.ArgumentList.Add(_endpoint.WebSocketEndpoint.GetLeftPart(UriPartial.Authority));
        start.Environment["CODEX_HOME"] = _codexHome;
        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start codex app-server.");

        for (var attempt = 0; attempt != ReadinessAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process.HasExited) throw new InvalidOperationException($"codex app-server exited with code {_process.ExitCode}.");
            if (await IsReadyAsync(cancellationToken)) return;
            await Task.Delay(ReadinessRetryDelay, cancellationToken);
        }
        throw new TimeoutException($"The managed App Server did not become ready at {_endpoint.ReadinessEndpoint}.");
    }

    public async ValueTask DisposeAsync()
    {
        // A normal Aura exit must not terminate a user-owned codex --remote TUI.
        // Ticket 3 transfers this process to the Guardian, which owns graceful drain.
        _process?.Dispose();
        _httpClient.Dispose();
    }

    private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try { using var response = await _httpClient.GetAsync(_endpoint.ReadinessEndpoint, cancellationToken); return response.IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }
}
