using System.Text.Json;

namespace AgentAura.Core.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    public const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonSettingsStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A settings path is required.", nameof(path));
        _path = path;
    }

    public static string GetDefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Agent Aura",
        "settings.json");

    public async Task<DurableSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return DurableSettings.Default;
        await using var stream = File.OpenRead(_path);
        var document = await JsonSerializer.DeserializeAsync<SettingsDocument>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException("Settings file is empty.");
        var settings = Migrate(document);
        settings.Validate();
        return settings;
    }

    public async Task SaveAsync(DurableSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, new SettingsDocument(CurrentSchemaVersion, settings), SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static DurableSettings Migrate(SettingsDocument document) => document.SchemaVersion switch
    {
        CurrentSchemaVersion => document.Settings,
        _ => throw new InvalidDataException($"Unsupported settings schema version {document.SchemaVersion}.")
    };

    private sealed record SettingsDocument(int SchemaVersion, DurableSettings Settings);
}
