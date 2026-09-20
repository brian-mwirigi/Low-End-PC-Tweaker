# Tweakwell

A small Windows tool for low-end PCs. Every change is shown before it is applied, logged, and reversible.

It will not turn a 2010 laptop into a gaming rig. It will not invent a “+40% FPS” number. It will not clean your registry, flush RAM, update drivers, or touch anti-cheat.

**Open source. No telemetry. No accounts. No ads. No bundled software.**

## What it does

1. **Scan** (read-only) — CPU, RAM, GPU (iGPU vs dedicated), storage type and free space, power plan, startup apps, and a raw process count. Findings are plain language: “4 GB RAM: expect heavy paging”, “the game may be running on the integrated GPU.”
2. **Tweaks** — each one has a description, a risk label, and a toggle. Power plan, Game Mode, Game Bar / Game DVR, animations, selected startup apps, temp files (shader cache is a separate opt-in), per-game dedicated GPU, per-game fullscreen optimizations.
3. **Preview** — a dry-run of every registry key, `powercfg` scheme, or folder that will change, with old and new values. Nothing runs until you confirm.
4. **Undo** — previous values are stored under `%LocalAppData%\Tweakwell`. Undo one tweak or restore everything. A Windows restore point is created before the first apply in a session when System Protection is on.
5. **Change log** — a readable history of what changed and when.
6. **Tip** — an About screen with [Buy Me a Coffee](https://buymeacoffee.com/brianmwirigi) and M-Pesa. One soft prompt after the first successful apply. No nagging.

Temp-file and shader-cache deletes cannot be undeleted. The preview says so.

## What it will not do

- Registry cleaners, RAM cleaners, “boost” buttons, fake issue counts
- Driver updates
- Writes inside game install folders
- Anything near Easy Anti-Cheat, BattlEye, or Vanguard
- Machine-wide Group Policy keys when a per-user setting works
- Network calls, except when you open a tip link

## Windows SmartScreen

Release builds are an **unsigned** unpackaged exe. Windows may show “Windows protected your PC.” That is SmartScreen reputation, not a detection. The source is this repo. Each GitHub Release attaches a **SHA-256** checksum; the release notes should also include a VirusTotal link after the maintainer uploads the exe.

## Requirements

- Windows 10 version 2004 (build 19041) or later, 64-bit
- To **run** a release: nothing else (self-contained)
- To **build**: [.NET 9 SDK](https://dotnet.microsoft.com/download), Windows 10/11 SDK, and the Windows App SDK workload (Visual Studio “Windows application development”)

This repo targets `net9.0-windows` because that is the SDK used to develop it. The published exe still bundles its own runtime.

## Build

```bash
dotnet test Tweakwell.sln
dotnet publish src/Tweakwell.App/Tweakwell.App.csproj -c Release -r win-x64 --self-contained
dotnet publish src/Tweakwell.Elevated/Tweakwell.Elevated.csproj -c Release -r win-x64 --self-contained
```

Copy `Tweakwell.Elevated.exe` next to `Tweakwell.exe`. The elevated helper is a separate process so the WinUI window never runs as Administrator.

Admin is requested only for a restore point or a power-plan switch, and the preview states why.

## Tip

If Tweakwell helped:

- [Buy Me a Coffee](https://buymeacoffee.com/brianmwirigi)
- M-Pesa: add the Till or phone in `src/Tweakwell.Core/SupportLinks.cs` (not published here yet)

## Later (not in this build)

Before/after idle RAM and process counts, optional PresentMon frametimes with a labeled raw result, per-game profiles, a temporary gaming-mode app closer, and profile import/export. Controller tweaks, a low-end preset library, and an in-app update check only if people ask.

## License

[MIT](LICENSE)
