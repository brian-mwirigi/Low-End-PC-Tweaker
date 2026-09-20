using Microsoft.Win32;

namespace Tweakwell;

public interface IRegistry
{
    bool KeyExists(RegistryHive hive, string path);
    IReadOnlyList<string> GetValueNames(RegistryHive hive, string path);
    string? GetString(RegistryHive hive, string path, string name);
    int? GetDword(RegistryHive hive, string path, string name);
    byte[]? GetBinary(RegistryHive hive, string path, string name);
    object? GetValue(RegistryHive hive, string path, string name);
    void SetString(RegistryHive hive, string path, string name, string value);
    void SetDword(RegistryHive hive, string path, string name, int value);
    void SetBinary(RegistryHive hive, string path, string name, byte[] value);
    void DeleteValue(RegistryHive hive, string path, string name);
}
