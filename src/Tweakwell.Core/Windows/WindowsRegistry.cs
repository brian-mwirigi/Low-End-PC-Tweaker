using Microsoft.Win32;

namespace Tweakwell;

public sealed class WindowsRegistry : IRegistry
{
    public bool KeyExists(RegistryHive hive, string path)
    {
        using var key = Open(hive, path, writable: false);
        return key is not null;
    }

    public IReadOnlyList<string> GetValueNames(RegistryHive hive, string path)
    {
        using var key = Open(hive, path, writable: false);
        return key?.GetValueNames() ?? [];
    }

    public string? GetString(RegistryHive hive, string path, string name)
    {
        using var key = Open(hive, path, writable: false);
        return key?.GetValue(name) as string;
    }

    public int? GetDword(RegistryHive hive, string path, string name)
    {
        using var key = Open(hive, path, writable: false);
        var value = key?.GetValue(name);
        return value is int i ? i : null;
    }

    public byte[]? GetBinary(RegistryHive hive, string path, string name)
    {
        using var key = Open(hive, path, writable: false);
        return key?.GetValue(name) as byte[];
    }

    public object? GetValue(RegistryHive hive, string path, string name)
    {
        using var key = Open(hive, path, writable: false);
        return key?.GetValue(name);
    }

    public void SetString(RegistryHive hive, string path, string name, string value)
    {
        using var key = OpenOrCreate(hive, path);
        key.SetValue(name, value, RegistryValueKind.String);
    }

    public void SetDword(RegistryHive hive, string path, string name, int value)
    {
        using var key = OpenOrCreate(hive, path);
        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    public void SetBinary(RegistryHive hive, string path, string name, byte[] value)
    {
        using var key = OpenOrCreate(hive, path);
        key.SetValue(name, value, RegistryValueKind.Binary);
    }

    public void DeleteValue(RegistryHive hive, string path, string name)
    {
        using var key = Open(hive, path, writable: true);
        if (key is null)
        {
            return;
        }

        try
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
        catch (ArgumentException)
        {
            // Missing value is already the desired end state.
        }
    }

    private static RegistryKey? Open(RegistryHive hive, string path, bool writable)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        return baseKey.OpenSubKey(path, writable);
    }

    private static RegistryKey OpenOrCreate(RegistryHive hive, string path)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        return baseKey.CreateSubKey(path, writable: true)
               ?? throw new InvalidOperationException($"Could not create registry key {hive}\\{path}.");
    }
}
