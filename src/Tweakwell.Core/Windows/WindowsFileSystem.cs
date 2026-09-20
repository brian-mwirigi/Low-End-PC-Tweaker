namespace Tweakwell;

public sealed class WindowsFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IReadOnlyList<string> EnumerateFiles(string path, bool recursive)
    {
        if (!Directory.Exists(path))
        {
            return [];
        }

        try
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(path, "*", option).ToList();
        }
        catch (Exception)
        {
            return [];
        }
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
}
