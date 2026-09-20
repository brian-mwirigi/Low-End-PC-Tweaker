namespace Tweakwell;

public sealed record GpuInfo(string Name, GpuKind Kind, long AdapterRamBytes);

public enum GpuKind
{
    Unknown,
    Integrated,
    Dedicated,
}

public sealed record PhysicalDiskInfo(string Name, StorageMedia Media, string Bus);

public enum StorageMedia
{
    Unknown,
    Hdd,
    Ssd,
}

public sealed record VolumeInfo(string Letter, long SizeBytes, long FreeBytes);

public sealed record StartupApp(
    string Name,
    string Command,
    StartupSource Source,
    bool Enabled,
    bool CanDisable);

public enum StartupSource
{
    CurrentUserRun,
    LocalMachineRun,
    StartupFolder,
}

public sealed record Finding(string Title, string Detail, FindingSeverity Severity);

public enum FindingSeverity
{
    Info,
    Warning,
}

public sealed record ScanResult(
    string CpuName,
    int LogicalProcessors,
    long TotalMemoryBytes,
    IReadOnlyList<GpuInfo> Gpus,
    IReadOnlyList<PhysicalDiskInfo> Disks,
    IReadOnlyList<VolumeInfo> Volumes,
    string PowerPlanName,
    string PowerPlanGuid,
    bool IsLaptop,
    int ProcessCount,
    IReadOnlyList<StartupApp> StartupApps,
    IReadOnlyList<Finding> Findings)
{
    public string MemorySummary
    {
        get
        {
            var gb = TotalMemoryBytes / (1024d * 1024d * 1024d);
            return $"{gb:0.#} GB";
        }
    }
}
