using Microsoft.Win32;

namespace Tweakwell.Tests;

internal sealed class FakeRegistry : IRegistry
{
    private readonly Dictionary<string, Dictionary<string, object>> _keys = new(StringComparer.OrdinalIgnoreCase);

    private static string KeyId(RegistryHive hive, string path) => $"{hive}\\{path}";

    public bool KeyExists(RegistryHive hive, string path) => _keys.ContainsKey(KeyId(hive, path));

    public IReadOnlyList<string> GetValueNames(RegistryHive hive, string path)
        => _keys.TryGetValue(KeyId(hive, path), out var values) ? values.Keys.ToList() : [];

    public string? GetString(RegistryHive hive, string path, string name)
        => GetValue(hive, path, name) as string;

    public int? GetDword(RegistryHive hive, string path, string name)
        => GetValue(hive, path, name) is int i ? i : null;

    public byte[]? GetBinary(RegistryHive hive, string path, string name)
        => GetValue(hive, path, name) as byte[];

    public object? GetValue(RegistryHive hive, string path, string name)
        => _keys.TryGetValue(KeyId(hive, path), out var values) && values.TryGetValue(name, out var value) ? value : null;

    public void SetString(RegistryHive hive, string path, string name, string value) => Set(hive, path, name, value);

    public void SetDword(RegistryHive hive, string path, string name, int value) => Set(hive, path, name, value);

    public void SetBinary(RegistryHive hive, string path, string name, byte[] value) => Set(hive, path, name, value);

    public void DeleteValue(RegistryHive hive, string path, string name)
    {
        if (_keys.TryGetValue(KeyId(hive, path), out var values))
        {
            values.Remove(name);
        }
    }

    private void Set(RegistryHive hive, string path, string name, object value)
    {
        var id = KeyId(hive, path);
        if (!_keys.TryGetValue(id, out var values))
        {
            values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _keys[id] = values;
        }

        values[name] = value;
    }
}
