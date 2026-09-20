namespace Tweakwell;

public sealed record TweakPlan(ITweak Tweak, IReadOnlyList<PlannedChange> Changes);

public sealed record ApplyPlan(
    IReadOnlyList<TweakPlan> Tweaks,
    IReadOnlyList<string> AdminReasons,
    bool NeedsRestorePoint)
{
    public IEnumerable<PlannedChange> AllChanges => Tweaks.SelectMany(t => t.Changes);

    public bool NeedsAdmin => AdminReasons.Count > 0;
}

public sealed record ApplyOutcome(
    bool Applied,
    SessionBackup? Backup,
    IReadOnlyList<string> Errors,
    bool ShowTip);
