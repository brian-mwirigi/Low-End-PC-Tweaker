using Microsoft.Win32;

namespace Tweakwell;

public sealed class VisualEffectsTweak : ITweak
{
    public const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    public const string Metrics = @"Control Panel\Desktop\WindowMetrics";

    private readonly IRegistry _registry;
    private readonly IClientAreaAnimation _animation;

    public VisualEffectsTweak(IRegistry registry, IClientAreaAnimation animation)
    {
        _registry = registry;
        _animation = animation;
    }

    public string Id => "visual-effects";
    public string Title => "Reduce animations and transparency";
    public string Description => "Turns off acrylic transparency and window animations. The desktop looks flatter; you get a bit less compositor work. Does not edit the UserPreferencesMask blob.";
    public TweakRisk Risk => TweakRisk.Low;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => true;
    public bool IsSelected { get; set; }

    public IReadOnlyList<PlannedChange> Preview()
    {
        var anim = _animation.GetEnabled();
        return
        [
            RegistryText.Dword(_registry, RegistryHive.CurrentUser, Personalize, "EnableTransparency", 0),
            RegistryText.String(_registry, RegistryHive.CurrentUser, Metrics, "MinAnimate", "0"),
            new PlannedChange("SystemParameters", "SPI_SETCLIENTAREAANIMATION", null, anim ? "1" : "0", "0"),
        ];
    }

    public void Apply()
    {
        _registry.SetDword(RegistryHive.CurrentUser, Personalize, "EnableTransparency", 0);
        _registry.SetString(RegistryHive.CurrentUser, Metrics, "MinAnimate", "0");
        _animation.SetEnabled(false);
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        foreach (var change in previous)
        {
            if (change.Target == "SystemParameters")
            {
                _animation.SetEnabled(change.OldValue != "0");
                continue;
            }

            if (change.ValueName == "EnableTransparency")
            {
                RegistryText.Restore(_registry, RegistryHive.CurrentUser, Personalize, change.ValueName, change.OldValue, StoredValueKind.DWord);
            }
            else if (change.ValueName == "MinAnimate")
            {
                RegistryText.Restore(_registry, RegistryHive.CurrentUser, Metrics, change.ValueName, change.OldValue, StoredValueKind.String);
            }
        }
    }
}
