namespace Tweakwell;

public sealed class TweakwellRuntime
{
    public LocalStore Store { get; }
    public ChangeEngine Engine { get; }
    public TweakCatalog Catalog { get; }
    public SystemScanner Scanner { get; }
    public ScanResult? LastScan { get; set; }

    public TweakwellRuntime(
        LocalStore store,
        ChangeEngine engine,
        TweakCatalog catalog,
        SystemScanner scanner)
    {
        Store = store;
        Engine = engine;
        Catalog = catalog;
        Scanner = scanner;
    }

    public static TweakwellRuntime CreateDefault()
    {
        var store = new LocalStore();
        var elevated = new ElevatedHelperClient();
        var hardware = new WmiHardwareProbe();
        var registry = new WindowsRegistry();
        var files = new WindowsFileSystem();
        var runner = new ProcessRunner();
        var catalog = new TweakCatalog(registry, runner, files, new ClientAreaAnimation(), elevated, hardware.IsLaptop);
        var engine = new ChangeEngine(store, elevated);
        var scanner = new SystemScanner(hardware, runner, registry, files);
        return new TweakwellRuntime(store, engine, catalog, scanner);
    }

    public ScanResult Scan()
    {
        LastScan = Scanner.Scan();
        return LastScan;
    }
}
