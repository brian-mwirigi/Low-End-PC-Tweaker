using Microsoft.Win32;

namespace Tweakwell;

public sealed class DedicatedGpuTweak : ITweak
{
    public const string Key = @"Software\Microsoft\DirectX\UserGpuPreferences";
    public const string HighPerformance = "GpuPreference=2;";

    private readonly IRegistry _registry;

    public DedicatedGpuTweak(IRegistry registry) => _registry = registry;

    public string Id => "dedicated-gpu";
    public string Title => "Force a chosen game onto the dedicated GPU";
    public string Description => "Writes HKCU UserGpuPreferences for one exe (GpuPreference=2). That is the same list as Windows Graphics settings. The game folder is never modified.";
    public TweakRisk Risk => TweakRisk.Low;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => true;
    public bool IsSelected { get; set; }
    public string ExecutablePath { get; set; } = "";

    public IReadOnlyList<PlannedChange> Preview()
    {
        EnsureSafe();
        var exe = Path.GetFullPath(ExecutablePath);
        return [RegistryText.String(_registry, RegistryHive.CurrentUser, Key, exe, HighPerformance)];
    }

    public void Apply()
    {
        EnsureSafe();
        _registry.SetString(RegistryHive.CurrentUser, Key, Path.GetFullPath(ExecutablePath), HighPerformance);
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        foreach (var change in previous)
        {
            RegistryText.Restore(_registry, RegistryHive.CurrentUser, Key, change.ValueName!, change.OldValue, StoredValueKind.String);
        }
    }

    private void EnsureSafe()
    {
        if (!AntiCheat.IsSafeGameExecutable(ExecutablePath, out var reason))
        {
            throw new InvalidOperationException(reason);
        }

        if (!File.Exists(ExecutablePath))
        {
            throw new InvalidOperationException("That .exe was not found. Choose the game executable.");
        }
    }
}
