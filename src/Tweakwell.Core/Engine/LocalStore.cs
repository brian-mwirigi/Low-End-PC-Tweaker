using System.Text.Json;

namespace Tweakwell;

public sealed class LocalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions JsonlOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _root;
    private readonly string _backups;
    private readonly string _changelog;
    private readonly string _settings;

    public LocalStore(string? root = null)
    {
        _root = root ?? AppPaths.Root;
        _backups = Path.Combine(_root, "backups");
        _changelog = Path.Combine(_root, "changelog.jsonl");
        _settings = Path.Combine(_root, "settings.json");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_backups);
    }

    public void SaveBackup(SessionBackup backup)
    {
        var path = Path.Combine(_backups, $"{backup.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(backup, JsonOptions));
    }

    public IReadOnlyList<SessionBackup> ListBackups()
    {
        if (!Directory.Exists(_backups))
        {
            return [];
        }

        return Directory.GetFiles(_backups, "*.json")
            .Select(ReadBackup)
            .Where(b => b is not null)
            .Cast<SessionBackup>()
            .OrderByDescending(b => b.AppliedAt)
            .ToList();
    }

    public SessionBackup? GetBackup(string id)
        => ReadBackup(Path.Combine(_backups, $"{id}.json"));

    public void AppendLog(ChangelogEntry entry)
    {
        var line = JsonSerializer.Serialize(entry, JsonlOptions);
        File.AppendAllText(_changelog, line + Environment.NewLine);
    }

    public IReadOnlyList<ChangelogEntry> ReadLog()
    {
        if (!File.Exists(_changelog))
        {
            return [];
        }

        var entries = new List<ChangelogEntry>();
        foreach (var line in File.ReadAllLines(_changelog))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<ChangelogEntry>(line, JsonlOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // skip a corrupt line
            }
        }

        entries.Reverse();
        return entries;
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settings))
        {
            return new AppSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settings), JsonOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
        => File.WriteAllText(_settings, JsonSerializer.Serialize(settings, JsonOptions));

    private static SessionBackup? ReadBackup(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SessionBackup>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
