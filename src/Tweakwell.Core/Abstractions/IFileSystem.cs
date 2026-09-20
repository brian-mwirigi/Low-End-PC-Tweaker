namespace Tweakwell;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    IReadOnlyList<string> EnumerateFiles(string path, bool recursive);
    FileWalk Summarize(string path, int maxFiles, int timeoutMs);
    FileWalk DeleteUnder(string path);
    long GetFileLength(string path);
    bool TryDeleteFile(string path, out long bytesDeleted);
    string Expand(string path);
}
