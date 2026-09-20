using System.Diagnostics;

namespace Tweakwell;

public sealed class WindowsFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IReadOnlyList<string> EnumerateFiles(string path, bool recursive)
    {
        if (!recursive)
        {
            return SafeEnumerate(path, recursive: false).Take(10_000).ToList();
        }

        return SafeEnumerate(path, recursive: true).Take(10_000).ToList();
    }

    public FileWalk Summarize(string path, int maxFiles, int timeoutMs)
    {
        if (!Directory.Exists(path))
        {
            return new FileWalk(0, 0, false);
        }

        var clock = Stopwatch.StartNew();
        var files = 0;
        long bytes = 0;
        var capped = false;

        foreach (var file in SafeEnumerate(path, recursive: true))
        {
            if (files >= maxFiles || clock.ElapsedMilliseconds >= timeoutMs)
            {
                capped = true;
                break;
            }

            files++;
            bytes += GetFileLength(file);
        }

        return new FileWalk(files, bytes, capped);
    }

    public FileWalk DeleteUnder(string path)
    {
        if (!Directory.Exists(path))
        {
            return new FileWalk(0, 0, false);
        }

        var deleted = 0;
        long bytes = 0;
        foreach (var file in SafeEnumerate(path, recursive: true))
        {
            if (TryDeleteFile(file, out var size))
            {
                deleted++;
                bytes += size;
            }
        }

        return new FileWalk(deleted, bytes, false);
    }

    public long GetFileLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public bool TryDeleteFile(string path, out long bytesDeleted)
    {
        bytesDeleted = 0;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return false;
            }

            bytesDeleted = info.Length;
            info.IsReadOnly = false;
            info.Delete();
            return true;
        }
        catch (Exception)
        {
            bytesDeleted = 0;
            return false;
        }
    }

    public string Expand(string path) => Environment.ExpandEnvironmentVariables(path);

    private static IEnumerable<string> SafeEnumerate(string path, bool recursive)
    {
        if (!Directory.Exists(path))
        {
            yield break;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint,
            BufferSize = 16 * 1024,
        };

        IEnumerator<string>? enumerator = null;
        try
        {
            enumerator = Directory.EnumerateFiles(path, "*", options).GetEnumerator();
            while (true)
            {
                bool moved;
                try
                {
                    moved = enumerator.MoveNext();
                }
                catch (Exception)
                {
                    yield break;
                }

                if (!moved)
                {
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator?.Dispose();
        }
    }
}
