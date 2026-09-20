namespace Tweakwell;

public sealed record TweakBackup(string TweakId, IReadOnlyList<PlannedChange> Changes);

public sealed record SessionBackup(
    string Id,
    DateTimeOffset AppliedAt,
    IReadOnlyList<TweakBackup> Tweaks,
    bool RestorePointAttempted,
    string? RestorePointMessage);

public sealed record ChangelogEntry(
    DateTimeOffset At,
    string Action,
    string TweakId,
    string Summary,
    string? SessionId);

public sealed class AppSettings
{
    public bool TipShown { get; set; }
}
