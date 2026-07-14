using System.Diagnostics;
using System.Net.Http;

namespace AgentAura.Prototype.BridgePrototype;

// PROTOTYPE: owns only the App Server process it starts.
internal sealed class CodexAppServerHost : IAsyncDisposable
{
    private readonly Uri _endpoint;
    private readonly string _codexHome;
    private readonly string _logPath;
    private readonly SemaphoreSlim _logLock = new(1, 1);
    private Process? _process;

    public CodexAppServerHost(Uri endpoint, string codexHome, string logPath)
    {
        _endpoint = endpoint;
        _codexHome = codexHome;
        _logPath = logPath;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_codexHome);
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);

        var startInfo = new ProcessStartInfo("codex")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("app-server");
        startInfo.ArgumentList.Add("--listen");
        startInfo.ArgumentList.Add(_endpoint.GetLeftPart(UriPartial.Authority));
        startInfo.Environment["CODEX_HOME"] = _codexHome;

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start codex app-server.");
        _ = PumpAsync(_process.StandardError, "stderr", cancellationToken);
        _ = PumpAsync(_process.StandardOutput, "stdout", cancellationToken);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var readiness = new UriBuilder(_endpoint) { Scheme = "http", Path = "/readyz" }.Uri;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"codex app-server exited with code {_process.ExitCode}.");
            }

            try
            {
                using var response = await http.GetAsync(readiness, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"App Server did not become ready at {readiness}.");
    }

    public ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }

        _process?.Dispose();
        _logLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task PumpAsync(StreamReader reader, string stream, CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                await _logLock.WaitAsync(cancellationToken);
                try
                {
                    await File.AppendAllTextAsync(_logPath, $"{DateTimeOffset.UtcNow:O} {stream} {line}{Environment.NewLine}", cancellationToken);
                }
                finally
                {
                    _logLock.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
