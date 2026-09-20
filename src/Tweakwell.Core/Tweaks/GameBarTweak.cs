using Microsoft.Win32;

namespace Tweakwell;

public sealed class GameBarTweak : ITweak
{
    public const string GameDvrKey = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    public const string ConfigKey = @"System\GameConfigStore";

    private readonly IRegistry _registry;

    public GameBarTweak(IRegistry registry) => _registry = registry;

    public string Id => "game-bar-dvr";
    public string Title => "Turn off Xbox Game Bar and Game DVR capture";
    public string Description => "Stops background capture used by Xbox Game Bar. Per-user only — no machine-wide policy key — so it stays undoable without admin.";
    public TweakRisk Risk => TweakRisk.Low;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => true;
    public bool IsSelected { get; set; }

    public IReadOnlyList<PlannedChange> Preview() =>
    [
        RegistryText.Dword(_registry, RegistryHive.CurrentUser, GameDvrKey, "AppCaptureEnabled", 0),
        RegistryText.Dword(_registry, RegistryHive.CurrentUser, ConfigKey, "GameDVR_Enabled", 0),
    ];

    public void Apply()
    {
        _registry.SetDword(RegistryHive.CurrentUser, GameDvrKey, "AppCaptureEnabled", 0);
        _registry.SetDword(RegistryHive.CurrentUser, ConfigKey, "GameDVR_Enabled", 0);
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        foreach (var change in previous)
        {
            var path = change.ValueName == "AppCaptureEnabled" ? GameDvrKey : ConfigKey;
            RegistryText.Restore(_registry, RegistryHive.CurrentUser, path, change.ValueName!, change.OldValue, StoredValueKind.DWord);
        }
    }
}
