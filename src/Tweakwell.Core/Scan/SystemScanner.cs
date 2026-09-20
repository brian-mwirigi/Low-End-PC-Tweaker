namespace Tweakwell;

public sealed class SystemScanner
{
    private readonly IHardwareProbe _hardware;
    private readonly IProcessRunner _runner;
    private readonly StartupEnumerator _startup;

    public SystemScanner(IHardwareProbe hardware, IProcessRunner runner, IRegistry registry, IFileSystem files)
    {
        _hardware = hardware;
        _runner = runner;
        _startup = new StartupEnumerator(registry, files);
    }

    public static SystemScanner CreateDefault()
        => new(new WmiHardwareProbe(), new ProcessRunner(), new WindowsRegistry(), new WindowsFileSystem());

    public ScanResult Scan()
    {
        var (planName, planGuid) = new PowerPlanReader(_runner).Active();
        var startup = _startup.List();
        var findings = FindingEngine.Build(
            _hardware.TotalMemoryBytes,
            _hardware.Gpus,
            _hardware.Disks,
            _hardware.Volumes,
            planName,
            _hardware.IsLaptop,
            startup);

        return new ScanResult(
            _hardware.CpuName,
            _hardware.LogicalProcessors,
            _hardware.TotalMemoryBytes,
            _hardware.Gpus,
            _hardware.Disks,
            _hardware.Volumes,
            planName,
            planGuid,
            _hardware.IsLaptop,
            _hardware.ProcessCount,
            startup,
            findings);
    }
}
