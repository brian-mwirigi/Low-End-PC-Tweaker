using Microsoft.Win32;

namespace Tweakwell;

public sealed class FullscreenOptimizationsTweak : ITweak
{
    public const string Key = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    public const string Flag = "~ DISABLEDXMAXIMIZEDWINDOWEDMODE";

    private readonly IRegistry _registry;

    public FullscreenOptimizationsTweak(IRegistry registry) => _registry = registry;

    public string Id => "fullscreen-optimizations";
    public string Title => "Disable fullscreen optimizations for a chosen game";
    public string Description => "Sets the same compatibility flag as Properties → Compatibility on that exe. Some games prefer this off; some do not. Try it, then undo if it looks worse. The game folder is never modified.";
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
        return [RegistryText.String(_registry, RegistryHive.CurrentUser, Key, exe, Flag)];
    }

    public void Apply()
    {
        EnsureSafe();
        _registry.SetString(RegistryHive.CurrentUser, Key, Path.GetFullPath(ExecutablePath), Flag);
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
