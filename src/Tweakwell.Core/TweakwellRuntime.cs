namespace Tweakwell;

public sealed class TweakwellRuntime
{
    private readonly IProcessRunner _runner;
    private readonly IRegistry _registry;
    private readonly IFileSystem _files;

    public LocalStore Store { get; }
    public ChangeEngine Engine { get; }
    public TweakCatalog Catalog { get; }
    public ScanResult? LastScan { get; set; }

    public TweakwellRuntime(
        LocalStore store,
        ChangeEngine engine,
        TweakCatalog catalog,
        IProcessRunner runner,
        IRegistry registry,
        IFileSystem files)
    {
        Store = store;
        Engine = engine;
        Catalog = catalog;
        _runner = runner;
        _registry = registry;
        _files = files;
    }

    public static TweakwellRuntime CreateDefault()
    {
        var store = new LocalStore();
        var elevated = new ElevatedHelperClient();
        var registry = new WindowsRegistry();
        var files = new WindowsFileSystem();
        var runner = new ProcessRunner();
        var catalog = new TweakCatalog(registry, runner, files, new ClientAreaAnimation(), elevated, isLaptop: false);
        var engine = new ChangeEngine(store, elevated);
        return new TweakwellRuntime(store, engine, catalog, runner, registry, files);
    }

    public ScanResult Scan()
    {
        var hardware = new WmiHardwareProbe();
        var scanner = new SystemScanner(hardware, _runner, _registry, _files);
        LastScan = scanner.Scan();
        Catalog.PowerPlan.IsLaptop = LastScan.IsLaptop;
        return LastScan;
    }

    public Task<ScanResult> ScanAsync() => Task.Run(Scan);
}
