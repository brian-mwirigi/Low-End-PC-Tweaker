using System.Diagnostics;
using System.Management;

namespace Tweakwell;

public sealed class WmiHardwareProbe : IHardwareProbe
{
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
        CpuName = ReadFirst("Win32_Processor", "Name") ?? "Unknown CPU";
        LogicalProcessors = Environment.ProcessorCount;
        TotalMemoryBytes = ReadLong("Win32_ComputerSystem", "TotalPhysicalMemory");
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

        if (n.Contains("amd") && n.Contains("radeon graphics") && !n.Contains("rx"))
        {
            return GpuKind.Integrated;
        }

        if (n.Contains("radeon(tm) graphics") || n.Contains("radeon graphics"))
        {
            return GpuKind.Integrated;
        }

        return GpuKind.Unknown;
    }

    private static IReadOnlyList<PhysicalDiskInfo> ReadDisks()
    {
        var list = new List<PhysicalDiskInfo>();
        foreach (var row in QueryNs(@"root\Microsoft\Windows\Storage", "MSFT_PhysicalDisk", "FriendlyName", "MediaType", "BusType"))
        {
            var name = row.GetValueOrDefault("FriendlyName") ?? "Disk";
            var media = ParseMedia(row.GetValueOrDefault("MediaType"));
            var bus = BusName(row.GetValueOrDefault("BusType"));
            list.Add(new PhysicalDiskInfo(name, media, bus));
        }

        if (list.Count > 0)
        {
            return list;
        }

        foreach (var row in Query("Win32_DiskDrive", "Model", "MediaType"))
        {
            var name = row.GetValueOrDefault("Model") ?? "Disk";
            var mediaText = row.GetValueOrDefault("MediaType") ?? "";
            var media = mediaText.Contains("SSD", StringComparison.OrdinalIgnoreCase) ? StorageMedia.Ssd
                : mediaText.Contains("HDD", StringComparison.OrdinalIgnoreCase) || mediaText.Contains("Fixed", StringComparison.OrdinalIgnoreCase)
                    ? StorageMedia.Unknown
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

    private static string BusName(string? raw) => raw switch
    {
        "17" => "NVMe",
        "11" => "SATA",
        "8" => "USB",
        _ => string.IsNullOrEmpty(raw) ? "unknown bus" : $"bus {raw}",
    };

    private static IReadOnlyList<VolumeInfo> ReadVolumes()
    {
        var list = new List<VolumeInfo>();
        foreach (var row in Query("Win32_LogicalDisk", "DeviceID", "Size", "FreeSpace", "DriveType"))
        {
            if (row.GetValueOrDefault("DriveType") != "3")
            {
                continue;
            }

            _ = long.TryParse(row.GetValueOrDefault("Size"), out var size);
            _ = long.TryParse(row.GetValueOrDefault("FreeSpace"), out var free);
            list.Add(new VolumeInfo(row.GetValueOrDefault("DeviceID") ?? "?", size, free));
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

        return Query("Win32_Battery", "Name").Count > 0;
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
        var rows = new List<Dictionary<string, string>>();
        try
        {
            var select = string.Join(", ", properties);
            using var searcher = new ManagementObjectSearcher(ns, $"SELECT {select} FROM {cls}");
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
        }
        catch (Exception)
        {
            // WMI is optional; callers tolerate empty lists.
        }

        return rows;
    }
}
