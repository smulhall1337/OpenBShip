# OpenBShip

A partial C# reconstruction of **Battleship: The Classic Naval Warfare Game (1997)** for modern Windows x64. The current game source is v0.34.

## Requirements

- Windows x64 with the .NET Framework 4.x compiler at `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`.
- An extracted copy of the supported English game. The folder must contain `Bs.exe`, `GRAPHICS`, `LWEBS`, `HWEBS` and `SFX`.

Original game assets and executables are **not included**. Setup checks the supplied files against the current asset manifest and extracts the small runtime programs and original cursor resources from `Bs.exe`. The supported executable has SHA-256 `B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569`.

## Build and launch

Clone the repository, open PowerShell in its folder, and run:

```powershell
git clone https://github.com/smulhall1337/OpenBShip.git
cd OpenBShip
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Setup.ps1 -OriginalRoot "C:\Games\Battleship"
.\Launch.cmd
```

Replace `C:\Games\Battleship` with the extracted game folder. Setup copies the required assets, verifies their hashes, extracts the runtime programs and builds `build\current\BattleshipLab.exe`. It preserves the supplied files. No ISO mount, original installer, registry configuration or additional game engine is needed to run the rebuild.

After setup, double-click **Launch.cmd** to play. Local `outputs/Inspect-*.cmd` tools display diagnostic reports. To rebuild after editing the source:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Close that build before rebuilding it. Settings are stored locally in `build\current\UserData\settings.txt`.

`Launch.cmd` waits for a key if setup or required assets are missing. Other startup failures show an error dialog and write details to `build\current\UserData\last-error.txt`.

## Playing

Use **Auto-place → Start battle**, or **Original menu → Solo → Classic → Start Game → Fleet Configuration → Start Game**.

| Control | Action |
| --- | --- |
| Mouse, or arrows + Enter | Choose a target |
| Click a friendly ship or its roster entry | Select the firing ship |
| P / Escape | Pause or resume battle |
| Space / Skip stage | Advance the current movie stage |
| F2 | Start a new battle |
| Options → Restart this battle… | Restore this battle's starting deployment |

Restart asks for confirmation and restores the actual starting fleets, rule preset, opponent setting and random streams, including manually placed fleets and repeated shuffles. Repeating the same actions reproduces the same gameplay and movie choices. This snapshot lasts for the current battle while the app is open; display and audio preferences keep their current values.

The Classic board uses the original targeting and hand cursor art. Untried enemy cells show a target, selectable friendly ships show a hand, and a shot or opponent turn shows a wait cursor over the board. Pause, resolved cells and finished battles use the arrow. These hover rules are an intentional usability policy; the shapes and target/wait transitions come from the original resources and code. Older prepared folders fall back to system cursors; rerun Setup to extract the original shapes.

Miss rings and the five original sinking clips now appear when a Classic shot resolves. **Animated damage effects** controls these transitions; turning it off clears active clips. They freeze during pause, and reset/restart clears them. The full-detail ship sprites use original frames and placement independently of the movie-detail setting. Rerun Setup after updating to import these resources.

Original intro replay, Credits, Quit, victory/defeat sequences and the movie archive are available. Cinematics play behind the board with the accepted 30% board-surface transparency. Campaigns, networking, tutorials, radar, controllers and the complete original lifecycle remain incomplete.

Local numerical comparisons now connect original fleet creation to player selection, firing, multiple turns, sinks and terminal callbacks. Tests cover 32 continuous numerical battles with two scripted player policies, also replayed through the original frame dispatcher with controlled decoder/clock/UI endpoints. Four battles additionally use real PAT data, original streaming/completion code and outbound command buffering with automatic flushes. File adapters are bounded; drawing/audio are stubbed and transport is disabled. Separate queue boundary tests use a recording transport. Earlier studies cover 900 controlled input cases including 768 first exchanges and a separate 560-case opponent study. One battle policy deliberately uses known enemy positions as test input. Actual original frame pacing, media/device callbacks and the complete shared random sequence remain unestablished; the rebuild retains separate gameplay and presentation random streams.

Canonical board picking is also checked at every integer pixel of the 640×480 viewport against original mouse-conversion code under both 53-bit and 64-bit x87 precision. The four PAT-driven numerical battles additionally pass scripted integer pixels through that conversion before original click dispatch, while preserving the earlier gameplay and random-state records. These comparisons use the fixed Classic camera and exclude later button/sprite hit dispatch and live camera setup.

Those battles also execute original camera easing, weapon-order and visual-entry creation/counter loops, and the Classic selection cache. The canonical camera stays fixed and weapon orders remain inactive. Separate bounded cases cover camera stepping/overshoot, entry allocation, counter retirement/wrap and draw-command geometry in all four orientations. The playable visual timeline matches the native traces; SPH scaling and placement match copied original software rasterization, including terminal slots and viewport edges. Active queued-weapon behavior and the complete original renderer remain incomplete. Modern effects freeze during pause even though native counters continue.

## Repository layout

- `src/`: current maintainable C# source, including the in-source diagnostic routines.
- `scripts/`: asset/program manifests and the import/extraction scripts needed by setup.
- `Setup.ps1`, `Build.ps1`, `Launch.cmd`: setup, compilation and launch entry points.

Generated `build/` content, assets, binaries, settings, old `outputs/` packages and local `work/` research are ignored. Only the current source is tracked; historical local builds are not required to launch a fresh checkout.
