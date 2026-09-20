namespace Tweakwell;

public sealed class ChangeEngine
{
    private readonly LocalStore _store;
    private readonly IElevatedOperations _elevated;
    private bool _restorePointThisSession;

    public ChangeEngine(LocalStore store, IElevatedOperations elevated)
    {
        _store = store;
        _elevated = elevated;
    }

    public ApplyPlan Preview(IEnumerable<ITweak> selected)
    {
        var tweaks = new List<TweakPlan>();
        var admin = new List<string>();

        foreach (var tweak in selected.Where(t => t.IsSelected))
        {
            var changes = tweak.Preview();
            if (changes.Count == 0)
            {
                continue;
            }

            tweaks.Add(new TweakPlan(tweak, changes));
            if (tweak.RequiresAdmin && !string.IsNullOrWhiteSpace(tweak.AdminReason))
            {
                admin.Add(tweak.AdminReason);
            }
        }

        var needsRestore = tweaks.Count > 0 && !_restorePointThisSession;
        if (needsRestore)
        {
            admin.Insert(0, "Create a Windows restore point before the first apply (if System Protection is on).");
        }

        return new ApplyPlan(tweaks, admin.Distinct(StringComparer.Ordinal).ToList(), needsRestore);
    }

    public Task<ApplyPlan> PreviewAsync(IEnumerable<ITweak> selected)
    {
        var snapshot = selected.ToList();
        return Task.Run(() => Preview(snapshot));
    }

    public ApplyOutcome Apply(ApplyPlan plan, bool createRestorePoint)
    {
        if (plan.Tweaks.Count == 0)
        {
            return new ApplyOutcome(false, null, ["Nothing to apply."], false);
        }

        string? restoreMessage = null;
        var restoreAttempted = false;
        if (createRestorePoint && !_restorePointThisSession)
        {
            restoreAttempted = true;
            var result = _elevated.CreateRestorePoint("Tweakwell before apply");
            restoreMessage = result.Message;
            if (result.Ok)
            {
                _restorePointThisSession = true;
            }
        }

        var errors = new List<string>();
        var applied = new List<TweakBackup>();

        foreach (var item in plan.Tweaks)
        {
            try
            {
                item.Tweak.Apply();
                applied.Add(new TweakBackup(item.Tweak.Id, item.Changes));
                _store.AppendLog(new ChangelogEntry(
                    DateTimeOffset.Now,
                    "apply",
                    item.Tweak.Id,
                    $"{item.Tweak.Title}: {item.Changes.Count} change(s)",
                    null));
            }
            catch (Exception ex)
            {
                errors.Add($"{item.Tweak.Title}: {ex.Message}");
            }
        }

        if (applied.Count == 0)
        {
            return new ApplyOutcome(false, null, errors, false);
        }

        var backup = new SessionBackup(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.Now,
            applied,
            restoreAttempted,
            restoreMessage);
        _store.SaveBackup(backup);
        _store.AppendLog(new ChangelogEntry(
            backup.AppliedAt,
            "session",
            string.Join(",", applied.Select(a => a.TweakId)),
            $"Saved undo snapshot {backup.Id}",
            backup.Id));

        var settings = _store.LoadSettings();
        var showTip = !settings.TipShown;
        if (showTip)
        {
            settings.TipShown = true;
            _store.SaveSettings(settings);
        }

        return new ApplyOutcome(true, backup, errors, showTip);
    }

    public Task<ApplyOutcome> ApplyAsync(ApplyPlan plan, bool createRestorePoint)
        => Task.Run(() => Apply(plan, createRestorePoint));

    public IReadOnlyList<string> UndoTweak(string tweakId, IEnumerable<ITweak> catalog)
    {
        var tweak = catalog.FirstOrDefault(t => t.Id == tweakId);
        if (tweak is null)
        {
            return [$"Unknown tweak '{tweakId}'."];
        }

        if (!tweak.IsReversible)
        {
            return [$"{tweak.Title} cannot be undone (files were deleted)."];
        }

        var errors = new List<string>();
        var found = false;
        foreach (var session in _store.ListBackups())
        {
            var match = session.Tweaks.FirstOrDefault(t => t.TweakId == tweakId);
            if (match is null)
            {
                continue;
            }

            found = true;
            try
            {
                tweak.Undo(match.Changes);
                _store.AppendLog(new ChangelogEntry(
                    DateTimeOffset.Now,
                    "undo",
                    tweakId,
                    $"Restored {match.Changes.Count} value(s) from session {session.Id}",
                    session.Id));
                break;
            }
            catch (Exception ex)
            {
                errors.Add($"{tweak.Title}: {ex.Message}");
            }
        }

        if (!found)
        {
            errors.Add("No backup found for that tweak.");
        }

        return errors;
    }

    public Task<IReadOnlyList<string>> UndoTweakAsync(string tweakId, IEnumerable<ITweak> catalog)
    {
        var snapshot = catalog.ToList();
        return Task.Run(() => UndoTweak(tweakId, snapshot));
    }

    public IReadOnlyList<string> RestoreAll(IEnumerable<ITweak> catalog)
    {
        var map = catalog.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var session in _store.ListBackups())
        {
            foreach (var item in session.Tweaks.Reverse())
            {
                if (!map.TryGetValue(item.TweakId, out var tweak))
                {
                    errors.Add($"Unknown tweak '{item.TweakId}'.");
                    continue;
                }

                if (!tweak.IsReversible)
                {
                    errors.Add($"{tweak.Title} cannot be undone (files were deleted).");
                    continue;
                }

                try
                {
                    tweak.Undo(item.Changes);
                    _store.AppendLog(new ChangelogEntry(
                        DateTimeOffset.Now,
                        "restore-all",
                        item.TweakId,
                        $"Restored {item.Changes.Count} value(s) from session {session.Id}",
                        session.Id));
                }
                catch (Exception ex)
                {
                    errors.Add($"{tweak.Title}: {ex.Message}");
                }
            }
        }

        return errors;
    }

    public Task<IReadOnlyList<string>> RestoreAllAsync(IEnumerable<ITweak> catalog)
    {
        var snapshot = catalog.ToList();
        return Task.Run(() => RestoreAll(snapshot));
    }
}
