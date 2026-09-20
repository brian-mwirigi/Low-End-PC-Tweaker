namespace Tweakwell;

public static class AppPaths
{
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tweakwell");

    public static string Backups => Path.Combine(Root, "backups");

    public static string Changelog => Path.Combine(Root, "changelog.jsonl");

    public static string Settings => Path.Combine(Root, "settings.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Backups);
    }

    public static bool IsAllowedHelperResultPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return false;
        }

        var allowed = new[]
        {
            Path.GetFullPath(Root),
            Path.GetFullPath(Path.GetTempPath()),
        };

        return allowed.Any(root => full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(full, root, StringComparison.OrdinalIgnoreCase));
    }
}
