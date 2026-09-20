using Microsoft.Win32;

namespace Tweakwell;

public sealed class GameModeTweak : ITweak
{
    public const string Key = @"Software\Microsoft\GameBar";

    private readonly IRegistry _registry;

    public GameModeTweak(IRegistry registry) => _registry = registry;

    public string Id => "game-mode";
    public string Title => "Turn on Windows Game Mode";
    public string Description => "Asks Windows to give the foreground game more CPU time and fewer background interruptions. Same toggle as Settings → Gaming → Game Mode.";
    public TweakRisk Risk => TweakRisk.Low;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => true;
    public bool IsSelected { get; set; }

    public IReadOnlyList<PlannedChange> Preview() =>
    [
        RegistryText.Dword(_registry, RegistryHive.CurrentUser, Key, "AutoGameModeEnabled", 1),
        RegistryText.Dword(_registry, RegistryHive.CurrentUser, Key, "AllowAutoGameMode", 1),
    ];

    public void Apply()
    {
        _registry.SetDword(RegistryHive.CurrentUser, Key, "AutoGameModeEnabled", 1);
        _registry.SetDword(RegistryHive.CurrentUser, Key, "AllowAutoGameMode", 1);
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        foreach (var change in previous)
        {
            RegistryText.Restore(_registry, RegistryHive.CurrentUser, Key, change.ValueName!, change.OldValue, StoredValueKind.DWord);
        }
    }
}
