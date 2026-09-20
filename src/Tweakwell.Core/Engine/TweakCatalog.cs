namespace Tweakwell;

public sealed class TweakCatalog
{
    public GameModeTweak GameMode { get; }
    public GameBarTweak GameBar { get; }
    public VisualEffectsTweak VisualEffects { get; }
    public PowerPlanTweak PowerPlan { get; }
    public StartupAppsTweak StartupApps { get; }
    public TempCleanTweak TempClean { get; }
    public DedicatedGpuTweak DedicatedGpu { get; }
    public FullscreenOptimizationsTweak FullscreenOptimizations { get; }

    public IReadOnlyList<ITweak> All { get; }

    public TweakCatalog(
        IRegistry registry,
        IProcessRunner runner,
        IFileSystem files,
        IClientAreaAnimation animation,
        IElevatedOperations elevated,
        bool isLaptop)
    {
        GameMode = new GameModeTweak(registry);
        GameBar = new GameBarTweak(registry);
        VisualEffects = new VisualEffectsTweak(registry, animation);
        PowerPlan = new PowerPlanTweak(runner, elevated, isLaptop);
        StartupApps = new StartupAppsTweak(registry);
        TempClean = new TempCleanTweak(files);
        DedicatedGpu = new DedicatedGpuTweak(registry);
        FullscreenOptimizations = new FullscreenOptimizationsTweak(registry);

        All =
        [
            PowerPlan,
            GameMode,
            GameBar,
            VisualEffects,
            StartupApps,
            TempClean,
            DedicatedGpu,
            FullscreenOptimizations,
        ];
    }

    public static TweakCatalog CreateDefault(bool isLaptop, IElevatedOperations? elevated = null)
        => new(
            new WindowsRegistry(),
            new ProcessRunner(),
            new WindowsFileSystem(),
            new ClientAreaAnimation(),
            elevated ?? new ElevatedHelperClient(),
            isLaptop);

    public bool TryGet(string id, out ITweak? tweak)
    {
        tweak = All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return tweak is not null;
    }
}
