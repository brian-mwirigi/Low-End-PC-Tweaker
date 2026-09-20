namespace Tweakwell.Tests;

public sealed class FindingEngineTests
{
    [Fact]
    public void FourGigRam_WarnsAboutPaging()
    {
        var findings = FindingEngine.Build(
            4L * 1024 * 1024 * 1024,
            [],
            [],
            [],
            "Balanced",
            isLaptop: false,
            []);

        Assert.Contains(findings, f => f.Title.Contains("4 GB RAM", StringComparison.Ordinal) && f.Title.Contains("paging"));
    }

    [Fact]
    public void DualGpu_WarnsGameMayUseIgpu()
    {
        var findings = FindingEngine.Build(
            16L * 1024 * 1024 * 1024,
            [
                new("Intel UHD Graphics", GpuKind.Integrated, 0),
                new("NVIDIA GeForce RTX 3060", GpuKind.Dedicated, 0),
            ],
            [],
            [],
            "Balanced",
            true,
            []);

        Assert.Contains(findings, f => f.Title.Contains("integrated GPU", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Hdd_WarnsAboutLoadTimes()
    {
        var findings = FindingEngine.Build(
            16L * 1024 * 1024 * 1024,
            [],
            [new("Disk", StorageMedia.Hdd, "SATA")],
            [],
            "Balanced",
            false,
            []);

        Assert.Contains(findings, f => f.Title.Contains("HDD"));
    }

    [Fact]
    public void PowerSaver_IsAFinding()
    {
        var findings = FindingEngine.Build(
            16L * 1024 * 1024 * 1024,
            [],
            [new("Disk", StorageMedia.Ssd, "NVMe")],
            [new("C:", 512L * 1024 * 1024 * 1024, 100L * 1024 * 1024 * 1024)],
            "Power saver",
            true,
            []);

        Assert.Contains(findings, f => f.Title.Contains("Power saver"));
    }
}
