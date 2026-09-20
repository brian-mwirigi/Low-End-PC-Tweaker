namespace Tweakwell;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    IReadOnlyList<string> EnumerateFiles(string path, bool recursive);
    long GetFileLength(string path);
    bool TryDeleteFile(string path, out long bytesDeleted);
    string Expand(string path);
}
