namespace Tweakwell;

public static class FindingEngine
{
    public static IReadOnlyList<Finding> Build(
        long totalMemoryBytes,
        IReadOnlyList<GpuInfo> gpus,
        IReadOnlyList<PhysicalDiskInfo> disks,
        IReadOnlyList<VolumeInfo> volumes,
        string powerPlanName,
        bool isLaptop,
        IReadOnlyList<StartupApp> startup)
    {
        var findings = new List<Finding>();
        var gb = totalMemoryBytes / (1024d * 1024d * 1024d);

        if (gb > 0 && gb < 4.5)
        {
            findings.Add(new Finding(
                $"{gb:0.#} GB RAM: expect heavy paging",
                "Windows will swap to disk when games and Chrome share this little memory. Close extra apps. Tweakwell cannot add RAM.",
                FindingSeverity.Warning));
        }
        else if (gb is >= 4.5 and < 8)
        {
            findings.Add(new Finding(
                $"{gb:0.#} GB RAM: keep background apps closed",
                "Playable for older titles if you are not running a browser zoo in the background.",
                FindingSeverity.Info));
        }

        var dedicated = gpus.Where(g => g.Kind == GpuKind.Dedicated).ToList();
        var integrated = gpus.Where(g => g.Kind == GpuKind.Integrated).ToList();
        if (dedicated.Count > 0 && integrated.Count > 0)
        {
            findings.Add(new Finding(
                "Game may be running on the integrated GPU",
                $"This machine has {NameList(integrated)} and {NameList(dedicated)}. Laptops often put a game on the iGPU unless you force the dedicated GPU for that exe.",
                FindingSeverity.Warning));
        }
        else if (dedicated.Count == 0 && integrated.Count > 0)
        {
            findings.Add(new Finding(
                "Only an integrated GPU was found",
                $"{NameList(integrated)} will run the game. Expect low settings. Tweakwell cannot add a graphics card.",
                FindingSeverity.Info));
        }

        if (disks.Any(d => d.Media == StorageMedia.Hdd))
        {
            findings.Add(new Finding(
                "HDD detected: expect long load times",
                "If the game lives on a spinning disk, installs and level loads will be slow. An SSD is the usual fix; Tweakwell will not pretend otherwise.",
                FindingSeverity.Warning));
        }

        var system = volumes.FirstOrDefault(v => v.Letter.StartsWith("C", StringComparison.OrdinalIgnoreCase));
        if (system is not null && system.SizeBytes > 0)
        {
            var freeGb = system.FreeBytes / (1024d * 1024d * 1024d);
            if (freeGb < 10)
            {
                findings.Add(new Finding(
                    $"{system.Letter} has {freeGb:0.#} GB free",
                    "Windows and shader caches need headroom. Clearing temp files can help a little; a bigger disk helps more.",
                    FindingSeverity.Warning));
            }
        }

        if (powerPlanName.Contains("power saver", StringComparison.OrdinalIgnoreCase)
            || powerPlanName.Contains("battery", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding(
                "Power saver is active",
                isLaptop
                    ? "The CPU and GPU will stay clocked down to save battery. High performance helps frames and kills battery life."
                    : "Power saver will cap performance on a plugged-in machine for no good reason.",
                FindingSeverity.Warning));
        }

        var enabledStartup = startup.Count(s => s.Enabled);
        if (enabledStartup >= 8)
        {
            findings.Add(new Finding(
                $"{enabledStartup} startup apps are enabled",
                "Each one costs boot time and idle RAM. Disable only ones you recognize.",
                FindingSeverity.Info));
        }

        if (findings.Count == 0)
        {
            findings.Add(new Finding(
                "No obvious hardware bottlenecks from this scan",
                "That is not a performance guarantee. Tweakwell only reports what it can see.",
                FindingSeverity.Info));
        }

        return findings;
    }

    private static string NameList(IEnumerable<GpuInfo> gpus)
        => string.Join(", ", gpus.Select(g => g.Name));
}
