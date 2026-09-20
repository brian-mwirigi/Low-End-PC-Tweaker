namespace Tweakwell.Tests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public string Output { get; set; } = "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)";

    public ProcessRunResult Run(string fileName, string arguments, int timeoutMs = 15000)
        => new(0, Output, "");
}

internal sealed class FakeFileSystem : IFileSystem
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Deleted { get; } = [];

    public bool DirectoryExists(string path) => Directories.Contains(path);

    public bool FileExists(string path) => Files.ContainsKey(path);

    public IReadOnlyList<string> EnumerateFiles(string path, bool recursive)
        => Files.Keys.Where(f => f.StartsWith(path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)
                                 || Path.GetDirectoryName(f)?.Equals(path, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

    public long GetFileLength(string path) => Files.TryGetValue(path, out var bytes) ? bytes.Length : 0;

    public bool TryDeleteFile(string path, out long bytesDeleted)
    {
        if (Files.TryGetValue(path, out var bytes))
        {
            bytesDeleted = bytes.Length;
            Files.Remove(path);
            Deleted.Add(path);
            return true;
        }

        bytesDeleted = 0;
        return false;
    }

    public string Expand(string path) => path.Replace("%TEMP%", @"C:\Users\test\AppData\Local\Temp", StringComparison.OrdinalIgnoreCase)
        .Replace("%LOCALAPPDATA%", @"C:\Users\test\AppData\Local", StringComparison.OrdinalIgnoreCase);
}

internal sealed class FakeAnimation : IClientAreaAnimation
{
    public bool Enabled { get; set; } = true;
    public bool GetEnabled() => Enabled;
    public void SetEnabled(bool enabled) => Enabled = enabled;
}

internal sealed class FakeElevated : IElevatedOperations
{
    public List<string> Calls { get; } = [];
    public bool RestoreOk { get; set; } = true;
    public bool PowerOk { get; set; } = true;

    public ElevatedResult CreateRestorePoint(string description)
    {
        Calls.Add("restore:" + description);
        return new ElevatedResult(RestoreOk, RestoreOk ? "ok" : "System Protection is off.");
    }

    public ElevatedResult SetPowerPlan(Guid schemeId)
    {
        Calls.Add("power:" + schemeId.ToString("D"));
        return new ElevatedResult(PowerOk, PowerOk ? "ok" : "failed");
    }
}

internal sealed class FakeHardware : IHardwareProbe
{
    public string CpuName { get; set; } = "Test CPU";
    public int LogicalProcessors { get; set; } = 4;
    public long TotalMemoryBytes { get; set; } = 4L * 1024 * 1024 * 1024;
    public IReadOnlyList<GpuInfo> Gpus { get; set; } =
    [
        new("Intel UHD Graphics", GpuKind.Integrated, 0),
        new("NVIDIA GeForce GTX 1650", GpuKind.Dedicated, 4L * 1024 * 1024 * 1024),
    ];
    public IReadOnlyList<PhysicalDiskInfo> Disks { get; set; } = [new("ST1000", StorageMedia.Hdd, "SATA")];
    public IReadOnlyList<VolumeInfo> Volumes { get; set; } = [new("C:", 256L * 1024 * 1024 * 1024, 20L * 1024 * 1024 * 1024)];
    public bool IsLaptop { get; set; } = true;
    public int ProcessCount { get; set; } = 180;
}
