# Contributing to Tweakwell

Tweakwell exists so every change is shown before it is applied, logged, and reversible. PRs that fight that promise will be closed.

## Hard no

Do not add any of the following, even behind a flag or “advanced” page:

- Telemetry, analytics, crash reporters that upload, accounts, ads, or bundled software
- Registry cleaners, RAM cleaners, “boost” / “optimize” / “fix all” buttons
- Fake issue counts, scores, or invented FPS claims
- Driver downloaders or updaters
- Writes into game install folders
- Anything that touches anti-cheat (Easy Anti-Cheat, BattlEye, Vanguard / `vgc` / `vgtray`, or their install paths)
- HKLM policy keys when an HKCU setting does the same job
- Raw command strings passed to the elevated helper

The elevated helper (`Tweakwell.Elevated`) is an allowlist. New operations must be a fixed verb plus validated arguments (GUIDs, booleans). Never `cmd /c` user input.

## How to add a tweak

1. Implement `ITweak` in `Tweakwell.Core` with a real `Preview()` that reports every registry value, `powercfg` scheme, or file path that will change, including the current value.
2. Give it a description and a `TweakRisk` (`Low` or `Caution`). Caution is required for battery impact, irreversible deletes, or first-launch stutter.
3. If it needs admin, set `RequiresAdmin` and `AdminReason`. The UI process must stay `asInvoker`.
4. If it cannot be undone (temp / shader cache), set `IsReversible` to false and say so in the preview.
5. Register it in `TweakCatalog`.
6. Cover Preview / Apply / Undo with a unit test against a fake registry or file system.

## How to run tests

```bash
dotnet test Tweakwell.sln
```

## Style

- C# / nullable enabled. No new dependencies without a reason that fits the trust rules.
- Keep the WinUI theme honest. No RGB “gamer booster” chrome.
