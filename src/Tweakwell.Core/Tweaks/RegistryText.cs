using Microsoft.Win32;

namespace Tweakwell;

internal static class RegistryText
{
    public static string HiveName(RegistryHive hive) => hive switch
    {
        RegistryHive.CurrentUser => "HKCU",
        RegistryHive.LocalMachine => "HKLM",
        _ => hive.ToString(),
    };

    public static string Format(object? value) => value switch
    {
        null => "(missing)",
        byte[] bytes => Convert.ToHexString(bytes),
        _ => Convert.ToString(value) ?? "(missing)",
    };

    public static PlannedChange Dword(IRegistry registry, RegistryHive hive, string path, string name, int desired)
    {
        var old = registry.GetDword(hive, path, name);
        return new PlannedChange("Registry", $"{HiveName(hive)}\\{path}", name, old?.ToString() ?? "(missing)", desired.ToString());
    }

    public static PlannedChange String(IRegistry registry, RegistryHive hive, string path, string name, string desired)
    {
        var old = registry.GetString(hive, path, name);
        return new PlannedChange("Registry", $"{HiveName(hive)}\\{path}", name, old ?? "(missing)", desired);
    }

    public static PlannedChange Binary(IRegistry registry, RegistryHive hive, string path, string name, byte[] desired)
    {
        var old = registry.GetBinary(hive, path, name);
        return new PlannedChange("Registry", $"{HiveName(hive)}\\{path}", name, Format(old), Format(desired));
    }

    public static void Restore(IRegistry registry, RegistryHive hive, string path, string name, string oldValue, StoredValueKind kind)
    {
        if (oldValue == "(missing)")
        {
            registry.DeleteValue(hive, path, name);
            return;
        }

        switch (kind)
        {
            case StoredValueKind.DWord:
                registry.SetDword(hive, path, name, int.Parse(oldValue));
                break;
            case StoredValueKind.Binary:
                registry.SetBinary(hive, path, name, Convert.FromHexString(oldValue));
                break;
            default:
                registry.SetString(hive, path, name, oldValue);
                break;
        }
    }

    public static RegistryHive ParseHive(string displayPath)
        => displayPath.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase)
            ? RegistryHive.LocalMachine
            : RegistryHive.CurrentUser;

    public static string StripHive(string displayPath)
    {
        var slash = displayPath.IndexOf('\\');
        return slash >= 0 ? displayPath[(slash + 1)..] : displayPath;
    }
}

internal enum StoredValueKind
{
    String,
    DWord,
    Binary,
}
