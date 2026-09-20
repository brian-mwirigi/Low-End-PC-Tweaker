# Tweakwell

Windows 10/11 app for low-end machines. You scan, pick tweaks, look at the old and new values, then apply. Backups and the change log live in `%LocalAppData%\Tweakwell`.

MIT. No telemetry or accounts. No ads.

This will not make an old laptop fast, and it does not print an FPS number. It also does not clean the registry, flush RAM, install drivers, or poke anti-cheat.

## Pages

**Scan** is read-only. CPU, RAM, GPUs (integrated vs dedicated), disks, free space, the active power plan, startup apps, process count. Findings look like “4 GB RAM: expect heavy paging.”

**Tweaks** — each row has a risk label:

- High-performance power plan (`powercfg`, needs admin)
- Game Mode
- Game Bar / Game DVR
- Animations and transparency
- Startup apps you check (per-user Run key / Startup folder)
- User `%TEMP%`. Shader cache is its own checkbox; games can hitch on the next launch after you clear it
- Per-game high-performance GPU (you pick the `.exe`)
- Per-game fullscreen optimizations off (same picker)

**Preview** lists every registry value, power scheme, or folder that would change. Nothing writes until you hit apply.

**History** can undo one tweak or the whole stack. Temp and shader-cache deletes cannot come back; preview already says that. The first apply in a session asks for a Windows restore point if System Protection is on.

**About** links to [Buy Me a Coffee](https://buymeacoffee.com/brianmwirigi). M-Pesa is in `src/Tweakwell.Core/SupportLinks.cs` and the number is still blank. After a first successful apply the app opens About once.

Admin is for a restore point or a power-plan switch. The UI process stays `asInvoker`. `Tweakwell.Elevated.exe` is a second process with a fixed verb list.

## Out of scope

No registry cleaner and no RAM cleaner. No driver updater. It does not write inside game install folders. Stay away from Easy Anti-Cheat, BattlEye, Vanguard. If an HKCU value does the job, leave HKLM policy alone. The only network use is if you click a tip link.

## SmartScreen

Release builds are an unsigned unpackaged exe, so Windows may show “Windows protected your PC.” New unsigned files trip SmartScreen. The source is this repo. Releases attach a SHA-256; add a VirusTotal link in the notes after you upload the exe.

## Requirements

- 64-bit Windows 10 2004 (build 19041) or newer
- A release exe does not need a separate .NET install
- To build: [.NET 9 SDK](https://dotnet.microsoft.com/download), a Windows 10/11 SDK, Visual Studio workload “Windows application development”

Projects target `net9.0-windows` because that is the SDK on the machine that builds them. The published exe still carries its own runtime.

## Build

```bash
dotnet test Tweakwell.sln
dotnet publish src/Tweakwell.App/Tweakwell.App.csproj -c Release -r win-x64 --self-contained
dotnet publish src/Tweakwell.Elevated/Tweakwell.Elevated.csproj -c Release -r win-x64 --self-contained
```

Copy `Tweakwell.Elevated.exe` next to `Tweakwell.exe`.

## Later

Maybe before/after idle RAM and process counts. PresentMon only if the number is labeled as raw. Per-game profiles and a “close these apps” list if they stay reversible. Import/export. Controller settings if someone needs them. Presets and an in-app update check only if people ask.

## License

[MIT](LICENSE)
