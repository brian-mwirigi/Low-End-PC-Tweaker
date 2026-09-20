namespace Tweakwell;

/// <summary>
/// Tweakwell never writes game folders and never touches anti-cheat binaries or their install paths.
/// GPU / fullscreen tweaks are HKCU rows keyed by a game exe path only.
/// </summary>
public static class AntiCheat
{
    private static readonly string[] DeniedPathFragments =
    [
        $"{Path.DirectorySeparatorChar}easyanticheat{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}battleye{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}riot vanguard{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}vanguard{Path.DirectorySeparatorChar}",
    ];

    private static readonly string[] DeniedFileNames =
    [
        "easyanticheat.exe",
        "easyanticheat_eos.exe",
        "beservice.exe",
        "battleye.exe",
        "vgc.exe",
        "vgtray.exe",
        "vgm.exe",
    ];

    public static bool IsDenied(string path, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            reason = "Path is empty.";
            return true;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            reason = "Path is not a valid file path.";
            return true;
        }

        var lower = full.ToLowerInvariant();
        var padded = lower.EndsWith(Path.DirectorySeparatorChar) ? lower : lower + Path.DirectorySeparatorChar;

        foreach (var fragment in DeniedPathFragments)
        {
            if (padded.Contains(fragment, StringComparison.Ordinal))
            {
                reason = "Refusing a path under an anti-cheat folder.";
                return true;
            }
        }

        var name = Path.GetFileName(full);
        if (DeniedFileNames.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Refusing an anti-cheat executable.";
            return true;
        }

        return false;
    }

    public static bool IsSafeGameExecutable(string path, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            reason = "Choose a game .exe first.";
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            reason = "Path is not a valid file path.";
            return false;
        }

        if (!string.Equals(Path.GetExtension(full), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            reason = "The path must be a .exe file.";
            return false;
        }

        if (IsDenied(full, out reason))
        {
            return false;
        }

        return true;
    }
}
