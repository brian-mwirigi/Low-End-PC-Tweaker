using System.Diagnostics;

namespace Tweakwell;

public sealed class TempCleanTweak : ITweak
{
    public static readonly string[] ShaderCacheRoots =
    [
        @"%LOCALAPPDATA%\NVIDIA\DXCache",
        @"%LOCALAPPDATA%\NVIDIA\GLCache",
        @"%LOCALAPPDATA%\AMD\DxCache",
        @"%LOCALAPPDATA%\AMD\GLCache",
        @"%LOCALAPPDATA%\D3DSCache",
    ];

    private readonly IFileSystem _files;

    public TempCleanTweak(IFileSystem files) => _files = files;

    public string Id => "temp-files";
    public string Title => "Clear temp files";
    public string Description => "Deletes files under your user %TEMP% folder. Locked files are skipped. This cannot be undone — Windows does not keep a recycle bin for these.";
    public TweakRisk Risk => TweakRisk.Low;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => false;
    public bool IsSelected { get; set; }
    public bool IncludeShaderCache { get; set; }

    public string ShaderCacheWarning =>
        "Clearing the shader cache can make the first launch of each game stutter while shaders compile again. It does not raise FPS after that.";

    public IReadOnlyList<PlannedChange> Preview()
    {
        var clock = Stopwatch.StartNew();
        const int budgetMs = 1_500;
        var changes = new List<PlannedChange>
        {
            Describe(_files.Expand("%TEMP%"), "User temp", Remaining(clock, budgetMs)),
        };
        if (IncludeShaderCache)
        {
            foreach (var root in ShaderCacheRoots)
            {
                changes.Add(Describe(
                    _files.Expand(root),
                    "Shader cache (cannot undo; first launches may stutter)",
                    Remaining(clock, budgetMs)));
            }
        }

        return changes;
    }

    public void Apply()
    {
        _files.DeleteUnder(_files.Expand("%TEMP%"));
        if (IncludeShaderCache)
        {
            foreach (var root in ShaderCacheRoots)
            {
                _files.DeleteUnder(_files.Expand(root));
            }
        }
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
        => throw new InvalidOperationException("Deleted temp files cannot be restored.");

    private PlannedChange Describe(string path, string label, int timeoutMs)
    {
        if (!_files.DirectoryExists(path))
        {
            return new PlannedChange("Folder", path, null, label + ": missing", "leave missing");
        }

        var walk = _files.Summarize(path, maxFiles: 8_000, timeoutMs: timeoutMs);
        var size = $"{label}: {(walk.Capped ? "at least " : "")}{walk.Files} files, {FormatBytes(walk.Bytes)}";
        if (walk.Capped)
        {
            size += " (preview stopped counting so the window stays awake)";
        }

        return new PlannedChange(
            "Folder",
            path,
            null,
            size,
            "delete unlocked files (cannot undo)");
    }

    private static int Remaining(Stopwatch clock, int budgetMs)
        => Math.Max(200, budgetMs - (int)clock.ElapsedMilliseconds);

    internal static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.#} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024d):0.#} MB";
        return $"{bytes / (1024d * 1024d * 1024d):0.#} GB";
    }
}
