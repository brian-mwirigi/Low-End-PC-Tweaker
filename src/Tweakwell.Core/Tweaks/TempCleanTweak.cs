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
        var changes = new List<PlannedChange> { Describe(_files.Expand("%TEMP%"), "User temp") };
        if (IncludeShaderCache)
        {
            foreach (var root in ShaderCacheRoots)
            {
                changes.Add(Describe(_files.Expand(root), "Shader cache (cannot undo; first launches may stutter)"));
            }
        }

        return changes;
    }

    public void Apply()
    {
        DeleteTree(_files.Expand("%TEMP%"));
        if (IncludeShaderCache)
        {
            foreach (var root in ShaderCacheRoots)
            {
                DeleteTree(_files.Expand(root));
            }
        }
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
        => throw new InvalidOperationException("Deleted temp files cannot be restored.");

    private PlannedChange Describe(string path, string label)
    {
        if (!_files.DirectoryExists(path))
        {
            return new PlannedChange("Folder", path, null, label + ": missing", "leave missing");
        }

        var files = _files.EnumerateFiles(path, recursive: true);
        long bytes = 0;
        foreach (var file in files)
        {
            bytes += _files.GetFileLength(file);
        }

        return new PlannedChange(
            "Folder",
            path,
            null,
            $"{label}: {files.Count} files, {FormatBytes(bytes)}",
            "delete unlocked files (cannot undo)");
    }

    private void DeleteTree(string path)
    {
        if (!_files.DirectoryExists(path))
        {
            return;
        }

        foreach (var file in _files.EnumerateFiles(path, recursive: true))
        {
            _files.TryDeleteFile(file, out _);
        }
    }

    internal static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.#} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024d):0.#} MB";
        return $"{bytes / (1024d * 1024d * 1024d):0.#} GB";
    }
}
