# OpenBShip

A partial C# reconstruction of **Battleship: The Classic Naval Warfare Game (1997)** for modern Windows x64. Current source: **v0.83**.

The project has a playable Classic battle, recovered menu and setup presentation, original media readers, and separate studies of recovered Practice systems. The studies include aircraft flight and AA on a supplied clock, plus a copied original installation with explicit recovery steps, inventory lookup and a separate prepared yard admission/supply/return study. Aircraft viewing includes a copied AWACS airfield, an optional recovered request-phase gate and bounded playback.

Original assets and executables are **not included**. This is an incremental reconstruction, with explicit limits on the original behavior established so far.

## Build and launch

Requires Windows x64, Git, and the .NET Framework 4.x compiler at `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`. No additional game engine or package manager is needed.

Supply an extracted English game folder containing `Bs.exe`, `GRAPHICS`, `LWEBS`, `HWEBS` and `SFX`:

```powershell
git clone https://github.com/smulhall1337/OpenBShip.git
cd OpenBShip
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Setup.ps1 -OriginalRoot "C:\Games\Battleship"
.\Launch.cmd
```

Replace `C:\Games\Battleship` with your extracted game folder. Setup copies and verifies 1,492 assets, extracts 41 runtime resources, and builds `build\current\BattleshipLab.exe`. It preserves the supplied files. The rebuild does not require an ISO mount, original installer, registry changes or administrator setup.

The supported RIP executable is **397,824 bytes**, SHA-256:

```text
B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569
```

The disc installer contains a different, unsupported 401,408-byte `Bs.exe` and a separate `BShip.Exe`. Their code and data layouts differ; addresses and extracted tables must not be transferred between builds.

After setup, double-click **Launch.cmd**. To rebuild edited source:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Close that build before rebuilding it; Build does not terminate games. Rerun Setup after updating from v0.81 or earlier to import all 41 runtime resources. Build only compiles source.

Settings and saved battles live in **`build\current\UserData`**. Preserve that folder when cleaning or moving the workspace. Only `build/current` is required to play. Temporary builds, private research and older comparison packages are optional. Startup errors are displayed and recorded in `build\current\UserData\last-error.txt`; Launch keeps its console open when setup or required files are missing.

## Playing

Use **Auto-place → Start battle**, or **Original menu → Solo → Classic → Start Game → Fleet Configuration → Start Game**.

| Control | Action |
| --- | --- |
| Mouse, or arrows + Enter | Choose a target |
| Friendly ship or roster entry | Select the firing ship |
| P / Escape | Pause or resume battle |
| Space / Skip stage | Advance the movie stage |
| F2 | Start a new battle |
| Options → Restart this battle… | Restore this battle's starting deployment |

Restart restores the starting fleets, rule preset, opponent setting and random streams, including manual placements and shuffles. Display and audio preferences retain their current values. The restart snapshot lasts while that battle is open.

**Options → Debug → Skip turn cinematics (debug)** resolves shots without loading turn videos while preserving damage, turn order and random choices. The setting persists and defaults off. Intro/result sequences and the clip library remain available. Normal turn movies appear behind the boards; board surfaces use the accepted 30% transparency during playback while grid lines, ships, markers and controls stay opaque.

## Shared aircraft and AA study

Open **Recovered maps → Shared flight / AA…**. This opens a separate copy of the first Practice map with fixed seed 143514006. It preserves the active battle, map preview and settings.

Presets deliberately supply attachments, equipment, ammunition, visibility, routes and resources. The updates use recovered rules; placements and schematic graphics are diagnostic inputs. The clock advances only through the controls. It does not establish original application pacing or complete AI behavior.

**Watch 32 polls (debug)** supplies one 40 ms step per Windows UI timer tick, then stops after 32. It uses the same updates as the manual controls. Under load, viewing can take longer; there is no catch-up. Click **Stop watching** to inspect the current state. Focus loss, selector changes, another action, reset or closing stop playback, with no automatic resume. This optional viewing aid starts off and does not change the recovered simulation rules.

### Manual flight and fire

The default helicopter preset is ready to use:

1. Click **Queue one AA order (raw)**.
2. Click **1 source poll (+40 ms)** to launch.
3. Click it again to reach the aircraft's first scheduler visit.
4. Use **32 source polls**, **128 source polls**, or **25 paused steps** to inspect progress.

Each normal poll runs two four-slot mixed-fleet batches before AA orders, projectiles and effects. Aircraft therefore move on scheduler visits rather than every poll. Paused steps hold flight, resources, requests and effects while compensating projectile deadlines. **Load / reset preset** restores a fresh copy. Selector changes take effect on Load.

### Recovered AA request phase

Enable **Generate AA on recovered phase 13**, set **Initial request counter** to **12**, and Load. One normal source poll advances the counter to 13, generates requests and launches admitted shots. The raw queue button is disabled while this option is applied. Paused steps hold the request counter. Starting at counter 0 delays the first producer pass until phase 13.

The producer shares the original random stream with impacts. Request generation can still produce orders that later fail firing admission. The preceding support helper is verified inactive for these presets; active kind-2 supply fleets remain unsupported. This option recovers a bounded source gate and producer, with other AI passes and mission orchestration still separate.

### AWACS return to a copied airfield

Choose **AWACS**, then **AWACS · supplied airfield**, and Load. With generated requests off, leave the raw queue empty:

- Two single polls move the AWACS from world **397,370** to **404,370**.
- One **32 source polls** click brings the total to 34 polls: it docks at **410,370**, becomes stored kind 8, and associates with home unit 100.
- Enable generated requests with initial counter 12 and Load to compare AA fire during that return.

The airfield's creation and placement are synthetic. Home selection, relinking, travel, docking and combat updates are checked against the original code. This preset requires AWACS; an incompatible Load reports the reason and preserves the current copy. Longer low-resource returns can exhaust resources before arrival, even without AA fire. Combat can also change an aircraft's fate.

## Original compound airfield constructor

The data-only constructor reproduces the original four-fleet, ten-unit installation from the pinned executable, including allocation, templates, ammunition and grid records. It is separate from the shared study's copied home and the prepared paired yard below. Original map placement and mission integration remain separate.

After running Setup for the new installation-template resource, export a standalone diagnostic copy:

```powershell
$exe = Join-Path $PWD 'build\current\BattleshipLab.exe'
$output = Join-Path $PWD 'build\airfield-export'
$run = Start-Process -FilePath $exe -ArgumentList @('--export-airfield-study', ('"{0}"' -f $output)) -Wait -PassThru
$run.ExitCode
Invoke-Item $output
```

Choose a new output directory each time; existing exports are preserved. The folder contains unit/fleet CSVs, four occupancy grids, complete normalized data pools, a hash manifest and a scope note. This uses supplied fresh pools, fleet slots 30–33, origin 400,370, owner 0 and flag 1. It does not change the running battle or saved settings. Original constructor comparisons cover 708 admitted cases; incomplete capacity, malformed resources and invalid destinations reject without changing caller pools.

## Installation recovery study

Open **Recovered maps → Installation study…** to inspect a fresh copy built by the original compound-constructor rules. The four grids keep the source convention: local X selects a row and local Y selects a column. Select a fleet and unit slot; the gold outline identifies its occupied cells.

The default unit is slot 0, source type 20, length 5:

1. **Supply mixed cells / HP 0 → Up to 16 recovered cell steps** applies 11 calls, changing cells 10 times. All of that unit's cells reach state 2 while HP remains 1.
2. **Reset → Supply one state 8 / HP length−1 → One recovered cell step** changes 8→7 and HP 4→5. A second step returns without changing state 7 because the source leaf stops when HP equals length.
3. **Find type in aligned four-fleet block**, with type 19, finds unit slot 2. Type 0 finds no matching referenced unit in this copy. Lookup is read-only.

The same study can run the recovered **startup pass** once per copied layout. Choose **Early diagnostic block 0–3**, set **Startup player count** to **1**, and **Load layout / player count**. Press **Run recovered startup pass (once)**: it fills one type-19 inventory and creates stored AWACS data at fleet 4, linked to home unit 0. This is stored data; flight does not run in this window. Select fleet 1, unit 2 and scroll to the details to inspect stocks such as IDs 14:80, 17:150 and 34:10.

The default later block 32–35 gets inventory but no AWACS for counts 0–4. The source uses base position and the supplied startup count for that eligibility. Only one installation is supplied; the count does not create other player bases. Layout/count changes remain pending until Load; Reset restores the applied construction before startup. The button disables after one pass because calling the original routine again can allocate another AWACS. The separate source API preserves that repeated-call behavior.
One recovered step changes only the first eligible cell: 5→2, 6→5, 7→6 or 8→7; only the last transition increments the signed HP field. This does not reconstruct the complete repair system or restore equipment records. Damage, location and explicit calls are diagnostic inputs. The general support caller remains guarded; constructor output alone does not establish a valid running installation. The separate paired yard below supplies the required roster explicitly.

## Prepared paired yard study

Open **Recovered maps → Installation study… → Paired yard study…**. This independent copy supplies one valid target/marker pair, an inventory depot and the source fleet. It does not change the parent installation, battle or settings.

With the default type-2 profile:

1. **Admit prepared ship** moves the gold target from source fleet 7 to yard fleet 34. The source fleet becomes empty; the marker saves its return mapping and the yard's original cells.
2. **One recovered worker pass** raises HP from 2 to 3 and transfers one ammunition type. The ship stays in the bay.
3. **Up to 20 passes / stop on return** completes the remaining calls and stops after **five total worker passes**. The ship returns to fleet 7, with its original coordinates and an intact source grid. Worker controls then disable.
4. **Reset applied profile** restores the prepared copy before admission.

Scroll to select other source types, marker capacities, supplied hull/ammunition/depot states or an escort. **Load selected profile** applies them. Selections remain pending until Load; Reset uses the applied profile. Type 1 needs capacity 5: loading it with a type-22 capacity-4 marker reports the invalid fit and preserves the current state. An empty depot releases the default ship on its first worker pass. An escort keeps the source fleet live while the target is away.

These are explicit calls with no world-clock advancement. One worker pass can change one damaged cell and transfer one ammunition type. With no applicable ammunition transfer, the recovered release path forces full HP and restores the source cells, even if intermediate cell recovery was incomplete. This reproduces the admitted source behavior; it is not a physical repair model.

Original comparisons cover **264 supplied histories / 5,808 complete pool observations**, using six real unit templates, fitting type-22/23 markers, three hull states, ammunition/depot variants and an optional escort. Three original display endpoints are adapted to count requests in the owned-image probes; actual original UI rendering is outside that comparison. The study shows those counts separately. General yard discovery, command admission in the running original application, multi-bay support, missions and the full AI remain unfinished. The shared-world kind-2 support guard remains in place.

## Other recovered studies

**Recovered maps** also provides:

- **Seeded deployment / Include map objects**: inspect original scenario records, grouped fleets and the first Practice terrain startup.
- **Waypoint study**: copied surface routes, terrain/collision outcomes and explicit scheduler polls.
- **Set up timed objects**: supplied creation, firing orders, projectile/impact/effect records, scores and optional fleet movement on a shared clock. Unsupported branches reject the entire update.
- **Aircraft study**: create, activate and dock one supplied attachment; inspect equipment, ammunition and available homes.
- **Waypoint flight study**, inside Aircraft study: copied routes and optional recovered resource-return visits, including an explicit test airfield. These visits have no established wall-clock duration.
- **Timed combat study**, inside Aircraft study: fixed-fleet AA launch/impact inspection, raw queues and explicit request-producer polls.

These studies have independent reset controls and preserve their parents. They do not execute complete original missions. The shared study's first five presets remain available, including moving batteries, resource expiry, carrier docking and clock wrap; AWACS has no airfield in those five presets.

## Reconstruction and modernization

| System | Current evidence and limits |
| --- | --- |
| Menus, setup and rendering | Runnable presentation using original assets and recovered rendering/text paths, with recorded source comparisons. Modern navigation, scaling, accessibility and input handling are deliberate choices. |
| Classic battle | Playable and repeatable. The reconstruction does not establish complete fidelity to every original mode, rule or orchestration path. |
| Resource and media formats | Maintained readers, pinned extraction manifests and reused original assets. Complete original media orchestration remains separate. |
| First Practice startup and movement | Bounded original comparisons for deployment, terrain, waypoint queues, collisions, fleet batches and carried scheduler state. Other maps and active secondary objects are not generally supported. |
| Aircraft and combat | Verified creation/modes, resources, available-home selection, return/docking, AA requests, launch, impacts, destruction and shared copied histories. A separate prepared yard recovers bounded admission/supply/return calls; general active supply, selected-aircraft UI transfer and complete attack tasks remain guarded or absent. |
| Full original application | Complete AI, missions/tutorial orchestration, network play and original input/application timing remain unfinished. A launched process or one passing study is not acceptance of a whole original battle. |

Source evidence uses the pinned executable identity above. Native probes execute admitted original routines or documented instruction fragments in an owned image with imports fenced; source addresses, copied inputs and adapted endpoints are recorded privately. Complete pool comparisons establish the recorded profiles, not every possible game state. Modern diagnostic presets and schematic UI remain labeled separately from recovered behavior.

AI cinematic upscaling remains stretch goal **SG01**; no upscaled assets are installed.

## Validation

For the v0.83 source, focused checks pass **348 model groups**. A fresh public checkout passes **278**, with **70 explicit skips** for optional private evidence. Form checks pass **128**; a fresh public form run can separately skip the optional private startup-palette comparison.

Run checks from PowerShell after Setup. Use `Start-Process -Wait` because these are GUI-subsystem executables:

```powershell
$exe = Join-Path $PWD 'build\current\BattleshipLab.exe'
$report = Join-Path $PWD 'build\validation.txt'
Start-Process -FilePath $exe -ArgumentList @('--self-check', ('"{0}"' -f $report)) -Wait
Get-Content $report
```

Use `--ui-check` with a different report path for real form/button checks. Those checks simulate events and use offscreen windows; they do not confirm physical input, audible playback or desktop focus behavior.

Recent private comparisons include 432 request-phase histories with 57,528 complete ten-pool observations, 126 broader airfield histories with 48,960 observations, and shorter manual/AA return fixtures. UI checks cover manual and generated requests, pause, reset, counter wrap, return, docking, resource expiry, pending settings and clean owner shutdown. Numeric sweeps and recorded observations are not counts of distinct gameplay scenarios.

A software comparison PNG can be exported with `--render-software-preview` and an output path. Other named comparison exports are listed in `src/Program.cs`.

## Repository layout

- `src/`: canonical maintained C# source.
- `scripts/`: asset manifests and preparation/extraction scripts.
- `Setup.ps1`, `Build.ps1`, `Launch.cmd`: reproducible setup, build and launch.
- `build/`: ignored generated runtime and local settings.
- `work/`: ignored analysis recipes, native evidence and intermediate files.
- `outputs/`: ignored local guides, reports and checked recovery archives.

Original media, binaries, user settings, historical builds and research artifacts are excluded from Git. If present, `outputs/Current-Status.txt` and the latest `work/checkpoint-*-handoff.txt` provide private continuity; a fresh checkout builds without them. Local maintenance keeps the current runtime and one previous comparison build, preserving user data and checked recovery paths when retiring temporary copies.
