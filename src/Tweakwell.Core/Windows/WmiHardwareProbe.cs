using System.Diagnostics;
using System.Management;

namespace Tweakwell;

public sealed class WmiHardwareProbe : IHardwareProbe
{
    private static readonly TimeSpan QueryLimit = TimeSpan.FromSeconds(2);

    public string CpuName { get; }
    public int LogicalProcessors { get; }
    public long TotalMemoryBytes { get; }
    public IReadOnlyList<GpuInfo> Gpus { get; }
    public IReadOnlyList<PhysicalDiskInfo> Disks { get; }
    public IReadOnlyList<VolumeInfo> Volumes { get; }
    public bool IsLaptop { get; }
    public int ProcessCount { get; }

    public WmiHardwareProbe()
    {
        LogicalProcessors = Environment.ProcessorCount;
        CpuName = ReadFirst("Win32_Processor", "Name") ?? "Unknown CPU";
        TotalMemoryBytes = ReadLong("Win32_ComputerSystem", "TotalPhysicalMemory");
        if (TotalMemoryBytes == 0)
        {
            TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        }

        Gpus = ReadGpus();
        Disks = ReadDisks();
        Volumes = ReadVolumes();
        IsLaptop = DetectLaptop();
        ProcessCount = SafeProcessCount();
    }

    private static int SafeProcessCount()
    {
        try
        {
            return Process.GetProcesses().Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static IReadOnlyList<GpuInfo> ReadGpus()
    {
        var list = new List<GpuInfo>();
        foreach (var row in Query("Win32_VideoController", "Name", "AdapterRAM"))
        {
            var name = row.GetValueOrDefault("Name") ?? "Unknown GPU";
            _ = long.TryParse(row.GetValueOrDefault("AdapterRAM"), out var ram);
            list.Add(new GpuInfo(name, ClassifyGpu(name), ram));
        }

        return list;
    }

    internal static GpuKind ClassifyGpu(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("basic display") || n.Contains("remote display") || n.Contains("microsoft remote"))
        {
            return GpuKind.Unknown;
        }

        if (n.Contains("nvidia") || n.Contains("geforce") || n.Contains("rtx") || n.Contains("gtx")
            || n.Contains("quadro") || n.Contains("intel arc") || n.Contains("radeon rx")
            || n.Contains("radeon pro") || (n.Contains("radeon") && (n.Contains("xt") || n.Contains("xtx"))))
        {
            return GpuKind.Dedicated;
        }

        if (n.Contains("intel") && (n.Contains("uhd") || n.Contains("iris") || n.Contains("hd graphics")
                                    || n.Contains("xe graphics") || n.Contains("graphics")))
        {
            return GpuKind.Integrated;
        }

        if (n.Contains("radeon") && n.Contains("graphics") && !n.Contains("rx") && !n.Contains("xt") && !n.Contains("pro"))
        {
            return GpuKind.Integrated;
        }

        return GpuKind.Unknown;
    }

    private static IReadOnlyList<PhysicalDiskInfo> ReadDisks()
    {
        // Storage WMI (MSFT_PhysicalDisk) hangs on some machines. Win32_DiskDrive is enough.
        var list = new List<PhysicalDiskInfo>();
        foreach (var row in Query("Win32_DiskDrive", "Model", "MediaType"))
        {
            var name = row.GetValueOrDefault("Model") ?? "Disk";
            var mediaText = row.GetValueOrDefault("MediaType") ?? "";
            var media = mediaText.Contains("SSD", StringComparison.OrdinalIgnoreCase) ? StorageMedia.Ssd
                : mediaText.Contains("HDD", StringComparison.OrdinalIgnoreCase) ? StorageMedia.Hdd
                : StorageMedia.Unknown;
            list.Add(new PhysicalDiskInfo(name, media, mediaText));
        }

        return list;
    }

    internal static StorageMedia ParseMedia(string? raw)
    {
        if (!int.TryParse(raw, out var code))
        {
            return StorageMedia.Unknown;
        }

        return code switch
        {
            3 => StorageMedia.Hdd,
            4 => StorageMedia.Ssd,
            _ => StorageMedia.Unknown,
        };
    }

    private static IReadOnlyList<VolumeInfo> ReadVolumes()
    {
        var list = new List<VolumeInfo>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                list.Add(new VolumeInfo(drive.Name.TrimEnd('\\'), drive.TotalSize, drive.AvailableFreeSpace));
            }
        }
        catch (Exception)
        {
            // DriveInfo can throw on locked volumes.
        }

        return list;
    }

    private static bool DetectLaptop()
    {
        foreach (var row in Query("Win32_SystemEnclosure", "ChassisTypes"))
        {
            var raw = row.GetValueOrDefault("ChassisTypes") ?? "";
            foreach (var token in raw.Split([',', '{', '}', ' '], StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out var type) && type is 8 or 9 or 10 or 11 or 14 or 30 or 31)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? ReadFirst(string cls, string property)
    {
        foreach (var row in Query(cls, property))
        {
            return row.GetValueOrDefault(property);
        }

        return null;
    }

    private static long ReadLong(string cls, string property)
    {
        _ = long.TryParse(ReadFirst(cls, property), out var value);
        return value;
    }

    private static List<Dictionary<string, string>> Query(string cls, params string[] properties)
        => QueryNs(@"root\cimv2", cls, properties);

    private static List<Dictionary<string, string>> QueryNs(string ns, string cls, params string[] properties)
    {
        try
        {
            var task = Task.Run(() => QueryNsCore(ns, cls, properties));
            return task.Wait(QueryLimit) ? task.Result : [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static List<Dictionary<string, string>> QueryNsCore(string ns, string cls, params string[] properties)
    {
        var rows = new List<Dictionary<string, string>>();
        var select = string.Join(", ", properties);
        using var searcher = new ManagementObjectSearcher(ns, $"SELECT {select} FROM {cls}");
        searcher.Options.Timeout = QueryLimit;
        searcher.Options.ReturnImmediately = true;
        foreach (var obj in searcher.Get())
        {
            using (obj)
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in properties)
                {
                    var value = obj[property];
                    row[property] = value switch
                    {
                        null => "",
                        Array arr => string.Join(",", arr.Cast<object>()),
                        _ => Convert.ToString(value) ?? "",
                    };
                }

                rows.Add(row);
            }
        }

        return rows;
    }
}
