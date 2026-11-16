# OpenBShip

A partial C# reconstruction of **Battleship: The Classic Naval Warfare Game (1997)** for modern Windows x64. Current source: **v0.91**.

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

Replace `C:\Games\Battleship` with your extracted game folder. Setup copies and verifies 1,609 assets, including the 121 supplied SCN maps, extracts 41 runtime resources, and builds `build\current\BattleshipLab.exe`. It preserves the supplied files. The rebuild does not require an ISO mount, original installer, registry changes or administrator setup.

The supported RIP executable is **397,824 bytes**, SHA-256:

```text
B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569
```

The disc installer contains a different, unsupported 401,408-byte `Bs.exe` and a separate `BShip.Exe`. Their code and data layouts differ; addresses and extracted tables must not be transferred between builds.

After setup, double-click **Launch.cmd**. To rebuild edited source:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Close that build before rebuilding it; Build does not terminate games. Rerun Setup after updating from v0.86 or earlier to import the SCN map library and all 41 runtime resources. Build only compiles source.

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

The data-only constructor reproduces the original four-fleet, ten-unit installation from the pinned executable, including allocation, templates, ammunition and grid records. It is separate from the shared study's copied home and the prepared paired yard below. The ordinary SCN base scan is exposed separately below; mission integration remains unfinished.

After running Setup for the new installation-template resource, export a standalone diagnostic copy:

```powershell
$exe = Join-Path $PWD 'build\current\BattleshipLab.exe'
$output = Join-Path $PWD 'build\airfield-export'
$run = Start-Process -FilePath $exe -ArgumentList @('--export-airfield-study', ('"{0}"' -f $output)) -Wait -PassThru
$run.ExitCode
Invoke-Item $output
```

Choose a new output directory each time; existing exports are preserved. The folder contains unit/fleet CSVs, four occupancy grids, complete normalized data pools, a hash manifest and a scope note. This uses supplied fresh pools, fleet slots 30–33, origin 400,370, owner 0 and flag 1. It does not change the running battle or saved settings. Original constructor comparisons cover 708 admitted cases; incomplete capacity, malformed resources and invalid destinations reject without changing caller pools.

The constructor can also use the recovered radial-origin planner. Supply a new export directory, a 32-bit unsigned seed, player count 1–4 and owner −1 through players−1:

```powershell
$exe = Join-Path $PWD 'build\current\BattleshipLab.exe'
$output = Join-Path $PWD 'build\radial-airfield-export'
$run = Start-Process -FilePath $exe -ArgumentList @('--export-radial-airfield-study', ('"{0}"' -f $output), '0', '2', '0') -Wait -PassThru
$run.ExitCode
Invoke-Item $output
```

This example chooses origin **390,370**, using two random draws with resulting state **505908858**, then constructs the four fleets. The 11-file export adds **Origin-Inputs.txt**, and its manifest covers the other ten files. Existing directories are preserved. The seed and empty reservation pools are supplied inputs; this does not reproduce a mission's earlier random history or install the result into gameplay.

The planner retains the source's stored angle constant, sector margins, radius range, integer truncation, 10-unit snapping and inclusive reservation boxes. Its maintained API also accepts complete primary and secondary reservation pools. A bounded retry failure preserves caller data and publishes no partial result. Original comparisons cover 10,752 seeded/reservation cases, 217,600 angle/radius/precision combinations and 210 complete origin-plus-constructor cases. Numerical combinations are not counts of playable scenarios. Terrain lookup, natural map startup and active support remain separate.

## Map installation construction

Open **Recovered maps → Map installations…**. The default **BSSB202.SCN** builds one compound installation: fleets 0–3, ten units, owner 1, origin **600,590**. Four tagged map cells produce one installation because the source checks existing fleet origins before creating another.

Select an installation and one of its four fleets, then open **Fleet grid** and choose a referenced unit slot. The gold outline follows that unit. World placement uses X across and Y down; fleet-grid rows use local X and columns use local Y. Both views are schematic modern presentation of recovered data.

Choose another SCN and press **Load map base pass**. A source selection stays pending until Load. **BSSB201.SCN** has no base tags and produces an empty result; **BSSB205.SCN** produces 14 installations, 56 fleets and 140 units. **BSSB418.SCN** is currently rejected with an invalid group count, preserving the applied copy. The unresolved file is retained without an assumption that it is corrupt.

This is the **ordinary base-construction stage only**. Scenario-specific constructors, other terrain entities, grouped fleets, postload stocks/aircraft, active support, AI and missions are excluded. Raw marker rosters remain unsuitable for the general support worker. The independent window preserves the active battle, parent map preview and settings.

Export the same data to a new directory:

```powershell
$exe = Join-Path $PWD 'build\current\BattleshipLab.exe'
$output = Join-Path $PWD 'build\map-installations-export'
$run = Start-Process -FilePath $exe -ArgumentList @('--export-map-installations', ('"{0}"' -f $output), 'BSSB202.SCN') -Wait -PassThru
$run.ExitCode
Invoke-Item $output
```

Use one SCN filename from `Assets\Graphics`. The default export contains 12 files: complete unit/fleet pools, installation/unit/fleet CSVs, four grids, source inputs, a scope note and a manifest covering the other 11 files. Existing files/directories are preserved. The maintained pass matches complete pools and owner counters from 120 original-code map scans: 494 compound installations across 1,976 tagged cells. These are data cases, not completed missions. No random draws occur in this stage.

### Copied spawn-zone study

In the same window, open **Spawn zones (study)**. It applies the recovered random-map zone rebuilding rule to supplied reservations: the constructed base fleets and up to 500 SCN kind-2 terrain cells. Other map objects and grouped fleets are not reserved. This copied profile does not run natural random-map or mission startup.

For the default **BSSB202.SCN**, the readout shows **4 base fleet reservations + 53 terrain cells**, **3,605 open cells**, and owners **0:1815 / 1:1790**. Blue/orange/green/violet show source owner sectors 0–3; dark cells are blocked, and gold follows the selected base. The player count comes from the SCN header. Fleet/unit selection changes only the highlight and details.

The rule excludes world edges 0 and 630 and reserves inclusive ±10 squares around every nonzero primary/secondary record, regardless of owner. It preserves the source's stored **57.2957725** angle constant and integer truncation. Replacing it with `180 / π` changes sector-boundary ownership. The routine uses no random draws. Its maintained implementation matches 48 complete original grids covering six reservation profiles, player counts 1–4 and two x87 precision settings; opaque blocked owner bytes and input data remain preserved. These are numerical/data cases, not missions. The mask is currently available through this study and its source API.

### Late configuration copy

In **Map installations…**, click **Late setup (copy)…**. This opens an independent copy of the applied map's base pass, even if another source is pending in the parent selector. It leaves the battle, parent map and spawn zones unchanged.

For **BSSB202.SCN**, choose fleet **2** to inspect the raw type-22 pairs. Click **Prepare late roster (once)**: unit slots **4, 6 and 8** become type `0` (empty), while odd markers, payloads, references and grids remain. The summary shows **3 markers cleared**, **0 carrier groups** and **next inactive fleet 4**. The button disables until **Reset this copy to base pass**. BSSB205 clears 42 markers; BSSB201 has none.

The separate owner input applies only when **Select owned fleet (copy)** is clicked. Owner **1** selects fleet 0/unit 0 in BSSB202; owner **0** falls back to inactive fleet 4/null unit. An absent owner preserves the prior selection. This retains the original preference for an owned kind-1 fleet, then the first owned record, including inactive records. Viewing a fleet and applying source selection are separate actions.

The maintained late pass also recovers carrier aircraft creation and per-owner kind-11 numbering. Original comparisons include complete carrier setup and repeated setup pools; a repeated raw call creates more stored aircraft groups. The once-per-copy UI guard is deliberate. This base-only viewer contains no grouped carriers, inventory startup, scenario callback, button layout, active yard work or missions. The general shared-world support guard remains. Its marker preparation matches complete pool outputs from 120 original-code map cases; these are data comparisons, not completed battles.

### BSSB202 startup stages

Apply **BSSB202.SCN** in Map installations, then click **BSSB202 startup (copy)…**. The button is available for that applied map; choosing a pending filename does not change its eligibility. This independent study builds primary unit/fleet data in maintained source, without executing the original game.

With supplied seed **731**, the stage selector shows:

| Stage | Nonempty fleets | Nonzero unit records | Random draws from seed |
| --- | ---: | ---: | ---: |
| Bases and map objects | 14 | 20 | 20 |
| Grouped fleets and stock | 23 | 42 | 432 |
| Practice fleet placement | 23 | 42 | 484 |
| Late roster and carrier data | 29 | 63 | 484 |

Choose **Map placement** or **Fleet grid**, then inspect a fleet and one of its six roster fields. Fleet 16 contains units 30 and 31. Stored aircraft groups share their parent's world origin; choose them through the fleet list. Gold identifies the selected fleet or nonempty unit. The snapshots retain the original coordinate conventions.

Changing the seed stays pending until **Load entered seed**. **Reset applied seed** rebuilds the applied seed, preserving any different pending value in the control. Reopening starts at 731. These snapshots do not advance a world clock or aircraft flight.

The Practice formation rule is separate from Classic placement: it retains the source's spacing, longest-first order, 100-attempt reset and random draw order. The maintained copy bounds retries and rejects invalid extents transactionally. Original comparisons cover 400 finite placement cases and five supplied BSSB202 seeds across four complete primary-pool stages. Numerical cases are not completed missions.

This startup subset excludes secondary terrain/auxiliary pools, SCN writeback, fleet-preset globals, scenario/display callbacks, AI and original application timing. The source's scenario-2 preset load leaves the compared primary pools and random state unchanged; its other effects are not reconstructed here. Other maps remain outside this startup API. The earlier base and spawn-zone studies keep their separate scope.

### Copied map yard continuation

In **BSSB202 startup (copy)…**, keep applied seed **731** and select stage **4**, then click **Copied map yard…**. The button is beside the stage selector. This opens another independent copy; other applied seeds and earlier stages keep the button disabled.

The default test uses unit **30**, the existing length-5 ship in fleet 16. It supplies one grid cell in state 8, HP 4 and empty ammunition. The recovered installation, paired markers and depot come from the reconstructed startup data. Yard selection **3** is an explicit test command; yard discovery and travel do not run.

1. Click **Enter copied yard (once)**. The target moves into bay fleet 3; its escort, unit 31, becomes selected in source fleet 16.
2. Click **One recovered worker pass**. HP reaches 5 and one weapon type receives ammunition. The unit is still in the bay.
3. Click **Up to 20 passes / stop on return**. It returns after **5 total worker passes**, with **138 ammunition** transferred from depot unit 2. The gold outline returns to its original source-grid position.
4. Change target/profile and click **Load selected test copy** to try another case. Changes stay pending until Load. **Reset applied test copy** restores the applied case while preserving pending selectors.

With empty ammunition, unit **31** returns after **4** passes with **162** transferred; unit **34** returns after **3** with **70** transferred. Both use the first fitting bay in fleet 2. Intact profiles with original ammunition return on the first pass. The mixed-grid profile is a supplied diagnostic state, not damage produced by an attack.

The source worker repairs at most one cell and transfers at most one weapon type per pass. A pass without an ammunition transfer returns the target and sets HP to its length; it does not wait for every copied grid cell to reach state 2. The study preserves this observed behavior. Gold marks the target and orange the yard marker; grids use local X as rows and local Y as columns. Details show saved return fields and counted display requests.

Original comparisons cover **nine supplied histories / 198 complete primary-pool and selection observations**, including 20 worker calls per case and stable calls after return. These are data comparisons, not completed missions. Only the three listed targets at seed 731 are admitted. Parent startup snapshots, the active battle and settings stay independent; general world support, multi-bay work, discovery/proximity, original display callbacks, AI and natural timing remain separate.

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

The read-only source gate displays **0 (available)** before admission, **1 (unavailable)** inside the bay, and **0** after return. The study still requires Reset before another admission. The gate checks signed yard selection, selected-yard ownership/kind, and a fitting empty marker pair; it does not check target damage or distance. Its result alone does not establish the stronger preconditions required by entry and support. The maintained gate matches 6,221 original synthetic field cases and guards reached malformed references. These cases are not counts of battles.

These are explicit calls with no world-clock advancement. One worker pass can change one damaged cell and transfer one ammunition type. With no applicable ammunition transfer, the recovered release path forces full HP and restores the source cells, even if intermediate cell recovery was incomplete. This reproduces the admitted source behavior; it is not a physical repair model.

Original comparisons cover **264 supplied histories / 5,808 complete pool observations**, using six real unit templates, fitting type-22/23 markers, three hull states, ammunition/depot variants and an optional escort. The copied profile starts with source fleet 7 selected and no selected unit. The native probes retain the original selected-unit maintenance called by two display endpoints, while counting requests and omitting remaining UI/camera rendering. With no escort, admission selects target unit 0 in yard 34; with an escort, source fleet 7 stays selected and switches to unit 1, then back to unit 0 on return. The details show this context and request counts. Selected-fleet fallback outside this prepared profile remains guarded. Earlier v0.83/v0.84 probes counted those endpoints without that selection continuation; the current comparison supersedes their selection scope. General yard discovery, command admission in the running original application, multi-bay support, missions and the full AI remain unfinished. The shared-world kind-2 support guard remains in place.

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

For the v0.91 source, focused checks pass **396 model groups**. A fresh public checkout passes **318**, with **78 explicit skips** for optional private evidence. Form checks pass **149**; a fresh public form run can separately skip the optional private startup-palette comparison.

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

Use `--render-map-yard-study` with a new output directory to export seven copied-yard form previews at default and minimum sizes. This uses the normal game executable, preserves existing output, and saves no settings. The previews establish layout and copied transitions, not physical display/input acceptance.

## Repository layout

- `src/`: canonical maintained C# source.
- `scripts/`: asset manifests and preparation/extraction scripts.
- `Setup.ps1`, `Build.ps1`, `Launch.cmd`: reproducible setup, build and launch.
- `build/`: ignored generated runtime and local settings.
- `work/`: ignored analysis recipes, native evidence and intermediate files.
- `outputs/`: ignored local guides, reports and checked recovery archives.

Original media, binaries, user settings, historical builds and research artifacts are excluded from Git. If present, `outputs/Current-Status.txt` and the latest `work/checkpoint-*-handoff.txt` provide private continuity; a fresh checkout builds without them. Local maintenance keeps the current runtime and one previous comparison build, preserving user data and checked recovery paths when retiring temporary copies.
