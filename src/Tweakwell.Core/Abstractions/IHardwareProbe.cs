namespace Tweakwell;

public interface IHardwareProbe
{
    string CpuName { get; }
    int LogicalProcessors { get; }
    long TotalMemoryBytes { get; }
    IReadOnlyList<GpuInfo> Gpus { get; }
    IReadOnlyList<PhysicalDiskInfo> Disks { get; }
    IReadOnlyList<VolumeInfo> Volumes { get; }
    bool IsLaptop { get; }
    int ProcessCount { get; }
}
