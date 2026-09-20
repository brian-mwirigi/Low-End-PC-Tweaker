using Microsoft.Win32;

namespace Tweakwell;

public sealed class StartupAppsTweak : ITweak
{
    private readonly IRegistry _registry;

    public StartupAppsTweak(IRegistry registry) => _registry = registry;

    public string Id => "startup-apps";
    public string Title => "Disable selected startup apps";
    public string Description => "Uses the same StartupApproved binary flags as Task Manager. Run key values are never deleted, so undo is a flag flip. Local Machine entries are listed on Scan and left alone.";
    public TweakRisk Risk => TweakRisk.Caution;
    public bool RequiresAdmin => false;
    public string? AdminReason => null;
    public bool IsReversible => true;
    public bool IsSelected { get; set; }

    public List<StartupPick> Selected { get; } = [];

    public IReadOnlyList<PlannedChange> Preview()
    {
        var when = DateTimeOffset.Now;
        var disabled = StartupEnumerator.DisabledBlob(when);
        return Selected.Select(pick =>
        {
            var path = StartupEnumerator.ApprovedPathFor(pick.Source);
            return RegistryText.Binary(_registry, RegistryHive.CurrentUser, path, pick.Name, disabled);
        }).ToList();
    }

    public void Apply()
    {
        var blob = StartupEnumerator.DisabledBlob(DateTimeOffset.Now);
        foreach (var pick in Selected)
        {
            var path = StartupEnumerator.ApprovedPathFor(pick.Source);
            _registry.SetBinary(RegistryHive.CurrentUser, path, pick.Name, blob);
        }
    }

    public void Undo(IReadOnlyList<PlannedChange> previous)
    {
        foreach (var change in previous)
        {
            var path = RegistryText.StripHive(change.Path);
            RegistryText.Restore(
                _registry,
                RegistryHive.CurrentUser,
                path,
                change.ValueName!,
                change.OldValue,
                StoredValueKind.Binary);
        }
    }
}

public sealed record StartupPick(string Name, StartupSource Source);
