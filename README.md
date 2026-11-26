# OpenBShip

A partial C# reconstruction of **Battleship: The Classic Naval Warfare Game (1997)** for modern Windows x64. Current source: **v0.107**.

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

Replace `C:\Games\Battleship` with your extracted game folder. Setup copies and verifies 1,609 assets, including the 121 supplied SCN maps, extracts 44 runtime resources, and builds `build\current\BattleshipLab.exe`. It preserves the supplied files. The rebuild does not require an ISO mount, original installer, registry changes or administrator setup.

The supported RIP executable is **397,824 bytes**, SHA-256:

```text
B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569
```

The disc installer contains a different, unsupported 401,408-byte `Bs.exe` and a separate `BShip.Exe`. Their code and data layouts differ; addresses and extracted tables must not be transferred between builds.

After setup, double-click **Launch.cmd**. To rebuild edited source:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

Close that build before rebuilding it; Build does not terminate games. Rerun Setup after updating from v0.97 or earlier to import all 44 runtime resources, including the AI preparation delay table. Build only compiles source.

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

The startup also retains a separate **53-record terrain pool**, with the other 447 slots inactive. Its complete 246,000 bytes match 20 original stage observations across the five recorded seeds. Other secondary/auxiliary pools, SCN writeback, fleet-preset globals, scenario/display callbacks, AI and original application timing remain excluded. The source's scenario-2 preset load leaves the compared primary pools and random state unchanged; its other effects are not reconstructed here. Other maps remain outside this startup API. The earlier base and spawn-zone studies keep their separate scope.

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

### Nearby movement into yard admission

In the same startup window at **seed 731 / stage 4**, choose **Nearby yard approach…**. This is a deliberately composed test of recovered movement, collision and admission. It supplies world position **622,600**, waypoint **610,600**, the selected hull/ammo profile and empty movement auxiliary data. The other fleets stay fixed; all 53 reconstructed BSSB202 terrain records are included.

With default unit **30**:

1. The collision field starts at **−1** and admission is unavailable. No admitted yard is supplied to the published approach state.
2. **One recovered movement visit** moves fleet 16 to **620,600**. Its box touches the installation; the source's strict overlap test still leaves admission unavailable.
3. Another visit proposes an overlap with base fleet **3**. Movement stays at 620,600, the outcome is **BlockedByFleet**, the collision field becomes **3**, and yard admission becomes available.
4. **Open admitted yard copy…** opens the existing yard study with target/profile fixed to the approach. Click **Enter copied yard (once)**, then apply worker passes as above. Closing this child preserves the approach. Reset restores its received position and stocks.

**Up to 20 visits / stop when admitted** reaches the same contact as manual visits. Unit 31 also needs two visits; faster fleet 18 carrying unit 34 contacts on its first visit. These are source movement visits, with no established wall-clock duration. Load applies pending target/profile choices; Reset restores the applied approach.

The individual terrain, movement, admission and worker rules have original comparisons. The composed cases additionally retain the nine recorded yard outcomes outside the deliberately changed world pose, waypoint and heading fields. **The full combined original approach has not been replayed.** This does not recover travel from the original spawn, general discovery/command UI, AI, multiple moving fleets or world timing. The earlier explicit-yard test remains available separately.

### Yard support phase copy

In either copied map yard, first click **Enter copied yard (once)**, then **Clock-gated yard copy…**. Open it before the target returns. This child receives the entered state and advances it separately; closing it leaves its manual parent unchanged.

The supplied initial counter is **12**. **One source poll** increments it to **13** and calls the recovered worker once. Each **64 source polls** action reaches the next worker phase: counters **77**, **141**, **205**, then **269**. For default unit 30 with empty ammunition, that fifth worker call returns the ship after **257 unpaused polls**, with the same 138-ammunition transfer as five manual worker calls.

**Pause source polls** holds both the phase counter and yard data, including when the counter is already at phase 13. Total/paused poll counts still show the requested observations. **Load counter / reset received copy** applies an entered counter and restores the received yard; **Reset applied test copy** retains the applied counter and leaves a different pending value untouched. The counter wraps at 32 bits. After return, further source phases leave the returned pools and display-request counts unchanged.

The source increments the counter only when unpaused, then gates support at `counter & 63 == 13`. This view composes that recovered gate with the recovered worker. The nine prior yard endpoints remain matched after twenty gated worker calls each; these are not new whole-original world replays. Other world systems, AA requests, AI, the application clock and automatic playback remain absent. No wall-clock duration is established by these manual polls.

### Fleet service task

In the startup window at **seed 731 / stage 4**, open **Fleet service task…**. This separate copy runs the recovered task-11/12 handler, ship-selection command, queue push/pop and shared-depot worker. Contact and tagged task records are supplied; there is no movement or full AI/world clock.

The default **Unit 30 · one state 8 / empty ammo** case starts with task 11:

1. **Visit initial recovered task** changes the task to 12 and admits unit 30 into bay fleet 3. The command chooses the ship; changing the view selector does not change source selection.
2. **One worker + task update** applies one worker pass, followed by one task visit.
3. **Up to 20 updates / stop on task 0** reaches queue kind 0 after five worker updates in this case. The ship returns with 138 ammunition. Kind 0 ends this supplied task slice; it does not establish mission completion.

Choose another case/task and **Load selected service copy** to apply it. Changes stay pending until Load; **Reset applied service copy** preserves pending choices. Gold follows the viewed original unit, and the lower bay selector switches between fleets 2 and 3. Native selection and queue records are shown separately in the details.

Orange dashed outlines identify the bay markers. Their blue cells remain after a ship returns; the gold outline follows the **viewed ship's current parent**. Choosing a viewed unit that is in a bay also selects that lower bay. Inspection never changes the simulation's selected ship.

**Fleet 16 · worse damage first** demonstrates the recovered choice rule: unit 30 has HP 0 and more grid damage, but the first visit admits unit 31 because it is the **last roster member with a positive grid-damage sum**. The next worker/task update admits unit 30 as well, temporarily leaving source fleet 16 inactive while both ships occupy bays. Returns recreate the source fleet as needed.

**Fleet 16 · two empty-ammo ships** admits both in one command. In the supplied worker-only continuation, shared stock can return unit 30 with **130**, rather than 138, ammunition. This study then revisits the task with contact deliberately held at 3, so it can repeatedly readmit a ship that still lacks stock. That is an observation of this controlled driver, not a diagnosis of natural original gameplay. All service cases stop at twenty worker/task updates; reset to repeat them.

The maintained API admits 19 supplied cases with initial task 11 or 12. It matches **38 selective-handler histories / 836 complete pool, task and selection observations**, including tagged queue payloads, opaque tail preservation, rejected contact, multiple admissions, source recreation and stock contention. The earlier command-only probe covers 18 overlapping cases. These counts are not completed battles. Other task kinds, the full AI controller, movement/clock orchestration, AA and original display work remain excluded. The general shared-world support guard is unchanged.

### Initialized service tasks

From the same applied **seed 731 / stage 4**, open **Initialized service tasks…**. This starts an independent copy using the recovered context layout and the reached paths of the original task dispatcher. **Fleet service task…** retains the tagged v0.94 baseline.

The mode selector offers **Tagged selective**, **Initialized selective**, and **Initialized dispatcher**. A mode change stays pending until **Load selected service copy**; Reset restores the applied mode and case. The initialized modes reproduce the original empty queue layout, actor link and initial task push. Untouched fields and the normalized table identity remain diagnostic inputs; this representation does not allocate the original AI heap or reproduce its entire initialization lifecycle.

The dispatcher processes friendly contact before the selective service handler. With the default task 11, unit 30 still returns after five worker updates with 138 ammunition, but route-clearing requests total **7**, compared with **2** in initialized selective mode. Those counts include the explicit initial task push. The queue and details show these intermediate effects.

To see the boundary, load **initial task 12** in initialized dispatcher mode and visit it. Friendly contact pops that lone task before service: the queue reaches 0 without moving the damaged ship. Initialized selective mode admits the ship because it runs the selective handler directly. These are different supplied entry states, not complete natural AI lifecycles.

Both initialized modes match **38 captured histories / 836 complete observations each**, including pool bytes, context bytes, selection, waypoints and display-request counts. The dispatcher uses supplied active AI owner 1 and rejects unsupported target, attachment-order and route-planning paths. The outer AI driver, planning policy, movement, AA, world clock, original heap and source UI remain unfinished. Updates still use the explicit manual worker/task cadence.

### Service phase clock

From the same **seed 731 / stage 4** startup copy, open **Service phase clock…**. This independent study separates the dispatcher and depot worker using the recovered source phase gates. The existing manual service windows remain available.

Start with the default case, task 11 and counter **0**:

1. **13 source polls** reaches counter 13. The first worker call has no admitted target; unit 30 stays at the source.
2. **Advance to next scheduled phase** reaches counter 32. The dispatcher admits unit 30 into bay 3.
3. Continue through scheduled phases. At counter **333**, the sixth worker call returns unit 30 with 138 ammunition; task kind remains 12. One of those six calls occurred before admission.
4. The next scheduled phase reaches **352**, where the dispatcher closes the queue to kind 0. Further polls continue: at counter **384**, the readout shows 12 task visits, 6 worker calls and 12 route requests.

**Pause source polls** holds the phase counter and service data. Requested and paused poll counts still advance within the **384-poll observation window**. The next-phase shortcut disables while paused. Reset restores the applied initial state and preserves the pending selectors and current Pause choice. Load applies a pending case, task or initial counter. The three cases and four initial counters are the captured inputs, including the unsigned wrap case 4,294,967,294.

The clock increments its unpaused unsigned counter first, dispatches when `counter & 31 == 0`, and runs support when `counter & 63 == 13`. It does not call the older combined worker/task operation. Read-only clock snapshots prevent mixing manual service actions into an ongoing clock history. Gold and orange use the same grid renderer as the manual service studies.

The maintained clock matches **48 original-fragment histories / 18,480 complete observations**, including paused phase holds, counter wrap, complete ship/fleet/task records, selection and request counts. The source experiment copies the original pause, increment and gate instructions and invokes the admitted dispatcher and worker. This remains a bounded phase subset: the outer AI driver, recurring policy, member orders, movement, AA, original heap, source display and natural wall-clock pacing are excluded. These are observation counts, not completed battles.

### Fleet candidate scan

From the **seed 731 / stage 4** startup copy, open **Fleet candidate scan…**. This is a separate inspection of the raw list built by the recovered lookup. It does not fire, move fleets or run the full AI policy. Its role within that policy remains unresolved.

The default surface case finds dispatch equipment but returns an empty list because the original eligibility bytes are zero. Set **Supply eligibility byte 1 on live fleets**, then **Load and scan selected copy**. The returned ids are **26, 25, 24, 23, 15, 14**, in source order. Gold marks the actor; cyan marks returned fleets; a white ring follows the viewed id. Selecting a result only changes inspection. Test positions can coincide; use the list to distinguish them.

Choose **Clear admitted equipment slots** and Load. The surface case pushes task **11**, clears five waypoints, and makes one counted route UI request. The stored aircraft case (kind 6) leaves the task unchanged without equipment. Reset rebuilds the applied result from the same inputs; selector changes remain pending until Load.

The scratch array starts at **-1**, then records roster positions for metadata bytes `F2`/`F3`. The next loop tests raw `!= 0`, which admits unmapped `-1` and excludes mapped position zero. Its range branch accepts **raw range <= Chebyshev distance**. Entry zero remains unmapped with raw range zero, so the supplied cases do not act as an inside-range firing test. These details are preserved as observed; they are not replaced with a guessed weapon rule.

The maintained factory matches **880 controlled original cases**, including full ship/fleet pools, normalized selected context, scratch/list bytes and selection/request results. Cases vary actor kind, parent category, equipment, eligibility, coordinates, active owner and scenario-8 ownership. The new 82-byte range span records the complete source read, extending beyond the previously extracted 35-entry firing table into adjacent data; it is deliberately named a **raw** span. No additional weapon meanings are inferred.

Positive matches for the category-1/3 target-kind filters, maximum list capacity, the full member-order dispatcher and recurring policy remain outside these captured cases. The owner-visibility producer is now recovered in a separate study below; it has not been joined to this candidate scan. The general running-world guards remain in place. The original code is not executed by this study or its ordinary checks.

### Owner visibility and packed history

From **BSSB202 startup → seed 731 / stage 4**, open **Owner visibility…**. The default prepares a separate copy for owner 1. Press **Apply one owner visibility pass**: fleet 14 stays at level 0 and fleet 15 receives level 1. Select a fleet to inspect its current level, observer far-contact byte and packed history before and after the pass. Reset restores the applied input; changed selectors remain pending until Load.

The copied-map presets retain the recovered positions and sensor radii. Controlled sensor and local-range presets instead supply positions, radii and history bytes explicitly. Marker colors show raw levels; coincident records share a marker and remain individually selectable in the list. This schematic view does not reproduce original fog graphics.

Normal passes clear current levels and far-contact flags. Near, middle and far radii are independent inclusive Chebyshev tests. Kinds 2, 10 and 25 also recover their owner's two history bits. The original upgrade comparison uses the shifted history byte before masking other owners' fields; this unusual behavior is preserved. In local mode 2, selecting no weapon returns before any clear, leaving prepared flags unchanged. With a selected weapon, original range and combat-table filters apply from the supplied selected-fleet origin. Observer kind 12 has a verified empty target-table row in this executable.

The pure producer matches **9,638 original before/after cases**; copied preset construction matches the **9,610 cases** represented by the window. These are numerical fixture cases, not completed battles. No firing, movement, whole owner loop or recurring AI runs. Existing candidate and member studies keep their supplied visibility inputs.

After completing either **Copied stage 4 · normal producer** or **Copied stage 4 · prefilled history** for owner 0 or 1, click **Scan produced-visibility candidates…**. This read-only child feeds the produced flags unchanged into the recovered raw candidate lookup, retaining the original equipment and map positions. On the natural copied map, owner 1 / actor 16 finds candidate 15; owner 0 / actor 14 finds candidate 9. Owner 0's explicit prefilled-history fixture returns 9, 3, 2, 0 in source scan order. Select an id to inspect its location, distance and produced flags; closing preserves the visibility parent.

The join matches **30 complete original observations** across two actors, three equipment profiles and five visibility/history fixtures. The public child admits only the four original-equipment copied-map combinations described above. Pending passes, other owners and controlled local/sensor fixtures keep the button disabled. Candidate membership does not establish numeric shot admission or an AI attack decision: the source raw range test and equipment-scratch sentinel behavior remain distinct from the firing rules. No orders or ammunition change in this child.

For owner 1, scroll the candidate child's right panel to **Prepare owner-1 members… (no firing)**. This opens an independent preparation copy using the produced flags, original equipment and ammunition. Equipment and eligibility selectors are locked for this path; task, difficulty, player-record override and the unresolved caller comparison word remain explicit supplied inputs. The earlier preparation entry below retains its existing supplied equipment and visibility presets.

With the joined defaults, press the next-visit button three times: at clock **12,345**, both members prepare against fleet 15 with due **14,845**; at **14,844**, they remain waiting; at **14,845**, both reach state 2 and updates stop before firing. Reset restores the applied copy, keeping pending selectors. The continuation matches **64 original histories / 224 complete observations**, including **160 post-visit candidate scratch/list states**. No firing requests, shot admission or ammunition consumption runs. This joins visibility to preparation under the displayed supplied context, without establishing the natural caller word, full owner loop or recurring AI policy. The owner-0 preparation entry stays disabled.

After completing that joined preparation with **difficulty 2, override 0 and caller word 0**, open **Copy to one request visit… (joined only)**. Task 0 or 8 and either admitted visibility preset are supported. This child holds the completed preparation and produced flags; its global weapon field and empty/nine/full tagged queue fixtures remain explicit inputs. The timer tags are comparison data, not recovered natural timer initialization.

Press **Queue one member visit**. Default task 0 / global 0 stores ten requests per source against fleet 15, using original selected weapons **18 and 17**. Both members then return to state 0. Global field 17 instead leaves states 0 / 1; field 18 gives 1 / 0; field 22 gives 1 / 1, with the same request cells. Task 8 prepares original weapon **22** for both and stores one request per source in empty rows. Select a list slot to follow its white ring; both sources use the single displayed target grid. Reset preserves pending global/queue selectors until Load.

The continuation matches **48 complete original cases**, including exact preparation endpoints, request pools and random state. A full request row can still consume random draws while retaining every queue byte. Requests do not establish actual shot admission, consume ammunition or allocate projectiles. The earlier supplied-equipment request window keeps its existing behavior; it has not been substituted for this joined path.

After the joined request visit with **global 0 and empty starting rows**, open **Inspect natural-distance consumer…**. This separate copy keeps the original map positions, equipment and observers. It supplies only the selected timer and projectile-capacity fixtures. The default retains diagnostic request timer tags: applying one pass clamps them to clock + 15,000 and holds the requests. These tags do not establish the original game's initial timers.

Select **Supply timer 0 (due) → Load → Apply one consumer pass**. With task 0, both F2 rows are rejected and cleared: distance 150 exceeds weapon 18's range 60 and weapon 17's range 20. Ammunition stays 30 / 150 and no record is allocated. With task 8, weapon 22 has range 375 and is admitted at the same positions: an empty pool allocates slots 0 / 1, clears the two requests and changes ammunition 8 / 8 to 7 / 7. One free slot allocates only slot 79 and uses ammunition from unit 30; a full pool still consumes the requests but uses no ammunition.

Timer equality holds requests; **one before clock** is due. Successful consumption writes the recovered next rate timer. Pending timer/capacity selections require Load; Reset restores the applied copy. The separate eligibility readout ignores the timer gate. This continuation matches **60 complete original cases**, including admission, all ship/fleet/request/projectile bytes, timers, ammunition and last-target identity. It runs no projectile motion, impact, damage, original media or full AI policy. Initial timer production and natural world pacing remain unresolved.

### Fleet member preparation

From the same **seed 731 / stage 4** startup, open **Member preparation…**. This separate copied prefix follows equipment choice, target assignment and waiting states for fleet 16. It stops before executing member state 2, which would enter the firing path. No shot or ammunition use occurs here.

Keep the defaults: F2 id 14 on both units, fleets 14 and 15 eligible, task 0, difficulty 2, player override 0 and comparison word 0. Press the numbered visit button three times:

1. At raw clock **12,345**, units 30 and 31 choose equipment 14 and targets **15** and **14**. Both enter waiting state 1, due at **14,845**.
2. At **14,844**, both remain waiting.
3. At **14,845**, both reach state 2. The button disables at this boundary. Reset repeats the applied prefix; pending selectors stay pending until Load.

The source initialization scales the two supplied preparation parameters by `2^difficulty`. These are raw clock values, with no wall-clock unit claimed:

| Difficulty | F2 delay | F3 delay | First visit with two eligible targets |
| --- | ---: | ---: | --- |
| 0 | 10,000 | 40,000 | Prepares the first member |
| 1 | 5,000 | 20,000 | Prepares the first member |
| 2 | 2,500 | 10,000 | Can prepare both members |
| 3 | 1,250 | 5,000 | Can prepare both members |

Equipment choice uses signed source combat scores and nonzero ammunition. The F3-only preset does not prepare under task 0; supplied task 8 admits it. Player-record override 1 replaces a waiting due field with 2, allowing the next visit to reach the guarded boundary earlier.

The waiting-state path also reads a caller stack comparison word before the state-0 path writes that local. Its natural caller value is unresolved. The study supplies **0 or 1** explicitly. To inspect the difference, choose difficulty 0 and only fleet 14 eligible: after two visits, word 0 allows the second member to prepare, while word 1 leaves it unprepared. This is a documented diagnostic dependency, not a chosen replacement AI policy.

The factory matches **768 controlled original histories / 2,928 complete observations** through this boundary, including complete pools, context, produced scratch/list data, selection and request fields. An independent maintained port also matches all-context hashes; a second controlled original capture is byte-identical. Initial scratch is unproduced and is not presented as recovered output. Unassigned target words remain opaque diagnostic data; weapon fields retain their previous value until a choice writes them.

Actual firing-state execution, attack queues, ammunition consumption, all other actors, natural caller stack values, recurring policy and complete world/AI timing remain unfinished. The general running-world guards remain in place.

### Fleet member request visit

Open **Member request visit…** from the same **seed 731 / stage 4** startup. This separate study builds the verified preparation prefix and permits **one** further whole member visit that stores firing requests. The preparation window above keeps its original stop-before-firing boundary.

With the defaults, the prefix has both members in state 2 at raw clock **14,845**, with empty request rows. Press **Queue one member visit**. Unit 30 queues ten cells against fleet 15; unit 31 queues ten against fleet 14. Cyan and orange crosses mark those requests on the copied target grids; a white ring follows the selected list slot. These crosses are **not launched shots or hits**. Ammunition, target data and the raw clock remain unchanged. Reset restores the applied prefix and queue fixture; selector changes remain pending until Load.

The three supplied raw visibility bytes select different recovered cell algorithms: byte 1 filters partial information, follows neighbors and searches random empty cells; byte 2 uses roster midpoint information; byte 3 scans occupied cells and then a fallback pass. Priority passes use source column order; the final full-grid fallback uses row order. Duplicate requests and retained queue fields are preserved. F2 can fill multiple slots; F3 stops after one enqueue attempt. F3-only equipment under task 0 is a negative control; task 8 admits it and the default visit then stores one request per unit.

The original member loop tests a **separate global weapon field** after storing requests. The study supplies 0, 14 or 19 explicitly. In the default F2 case, global 0 leaves both member states at 0 afterward; global 14 leaves them at 1, with identical request rows. This field is not silently replaced by each unit's chosen weapon. Caller comparison, raw visibility, fixed RNG seed 731 and prefilled queue rows are also explicit controlled inputs. The 9/10-slot fixtures preserve diagnostic Y tags outside the 0–9 grid; those entries remain in the list and are not plotted as cells.

The canonical continuation matches **648 whole original cases**, including complete ship/fleet/context/request pools, produced scratch/list bytes, RNG and selection/request context. Of these, 540 reach the request-producing state and 108 are F3/task-0 negative cases. The cell selector additionally matches **1,260 original helper cases**, including empty, nearly full, full, kind-change and target-change rows. These are controlled contexts, not completed battles.

This window joins only request storage. The separate local-launch study below continues two copied request presets through allocation. Impact, damage, compound targets, recurring AI policy and natural hidden-input production remain outside this member continuation. Existing projectile/impact studies have not been composed with it into a complete Practice battle.

### Generated requests to local launch

From the **seed 731 / stage 4** startup, open **Requests → local launch…**. The default loads generated F2 requests with explicit copied fixtures: both target origins at exact source range **20**, observer fleet 249 with owner 1 and radii 255, zero order timers and an empty projectile pool. These overrides make the two sources admitted; they do not establish natural AI visibility or timer setup.

Press **Consume requests / one local launch pass**. Units 30 and 31 each lose one queued request and one round: requests become **9 / 9**, ammunition **6 / 6**, and two numeric projectile records appear in the list. Selecting a record shows its stored start, target offsets and deadline. The schematic's dashed lines show stored destinations; the records do not move in this window.

Choose **One free projectile slot** and Load, then run the pass: source 30 receives slot **79**, source 31 allocates nothing, and ammunition becomes **6 / 7**. Both requests are still consumed. With **Full projectile pool**, neither allocates or uses ammunition, but both requests are consumed. The occupied kind-99 records are capacity fixtures and are never stepped. The F3 preset preserves the copied map positions and starts with one request per unit; successful allocation leaves both request rows empty.

Reset restores the applied copy before consumption, keeping different pending selectors. Only one pass runs per copy. All six before/after contexts match complete original ship, fleet, request and timed-object snapshots plus last-target data. This establishes numeric allocation and ammunition behavior in those contexts, with UI/media callbacks excluded. Projectile motion, impacts, damage, actual movies/audio and complete AI/world pacing remain separate.

### Copied pre-impact flight

After completing an **empty-pool** local launch above, click **Open pre-impact flight…**. This opens an independent copy of those two projectiles. Nearly full and full capacity fixtures keep the button disabled because their occupied test records are unsuitable for motion.

**1 numeric update (+40)** supplies one clock step and prior delta 40. **Up to 32 updates** stops when another full step would reach impact. The original integer rounding can leave a coordinate unchanged on a visit. Cyan and orange points show current positions, faint dots mark launch positions, and a white ring follows the viewed record. Dashed lines show the remaining destination vectors. Fleets stay fixed.

**Advance to first deadline − 1** applies normal steps and then an explicit shorter clock jump, retaining prior delta 40. F2 stops at **16,344**, with positions **245,423** and **250,428**. F3 stops at **28,944**, with positions **307,382** and **325,381**. A quotient-zero record stops the entire original pass, leaving later records unchanged. The controls never enter the first impact; F3 weapon 19's impact branch remains unsupported.

Reset restores the copied launch without changing the parent, active battle or settings. Ammunition, requests, ships and RNG hold throughout this future-only study. Its complete 40-unit histories match **205 original observations**; the underlying motion API separately matches eight captured histories with 8,841 observations across four supplied prior deltas. These are controlled numerical histories, not complete battles or proof of natural wall-clock pacing, impacts, damage or media playback.

### F2 water misses and effect lifetime

After an **empty-pool F2** local launch, open **F2 miss / effect history… (from launch)**. This starts another independent copy at the launch clock. It advances the two generated projectiles through their water impacts and recovered effect counters, while holding the remaining nine firing requests per source. The F3 and capacity-fixture cases keep this entry disabled.

Keep **Uniform +40 updates** and press **Next transition**:

1. Clock **16,365**: one projectile remains and the first water effect appears.
2. Clock **17,245**: both projectiles have retired and two effects are active.
3. Clock **17,645**: the first effect retires.
4. Clock **18,525**: the copied projectile/effect history is complete.

The blue rings are modern schematic geometry; their underlying type, position and lifetime counters use recovered data. Select an effect to inspect those fields. Reset restores the applied timing copy; a different pending timing selection takes effect only on Load. The three diagnostic modes replace the first clock visit with an exact or late deadline, then use +40 updates. They preserve the source callback-before-return behavior and retired coordinate bytes.

All four histories match **241 complete original observations**, including ship, fleet, timed, request, effect and score pools, RNG, movie counter and fixed counter/timestamp context. The movie-choice RNG advances even though no movie plays. Both target cells are water, so this does not reach hit, defense, damage or destruction. Ammunition stays at 6 / 6 after launch. Further firing, F3 impacts, moving fleets, recurring AI and complete battles remain separate.

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

For the v0.107 source, focused checks pass **495 model groups**. A fresh public checkout passes **401**, with **94 explicit skips** for optional private evidence. Form checks pass **213**; a fresh public form run can separately skip the optional private startup-palette comparison.

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

`--render-yard-approach-study` similarly exports the nearby approach, contact and received yard copy at default and minimum sizes.

`--render-yard-phase-study` exports the entered-yard phase, pause, return and scrolling controls through the same application.

`--render-fleet-service-study` exports the selective service task, queue completion and two-bay cases.

`--render-initialized-service-study` exports initialized dispatcher admission, return, lone-task-12 behavior, bay markers and scrolling layouts.

`--render-service-phase-study` exports phase-gated admission, separate return and queue-completion points, pause and wrap at default and minimum sizes.

`--render-fleet-candidates` exports original/supplied eligibility, the missing-equipment task effect and base alignment, including minimum-size controls and details.

`--render-member-preparation` exports equipment choice, deadline checks, the guarded firing boundary and supplied comparison-word cases at default and minimum sizes.

`--render-member-requests` exports prepared and queued request rows, alternate selectors and F3 behavior, including minimum-size controls and lists.

`--render-member-launch` exports copied request consumption and numeric allocation for empty, nearly full and full projectile pools, including minimum-size controls and details. It does not animate flight or impacts.

`--render-member-flight` exports F2/F3 launch positions, 32 supplied updates, the last pre-impact clock and minimum-size details. Impact remains guarded.

`--render-member-misses` exports the copied F2 water impacts, schematic effects, completion and diagnostic deadline timing, including minimum-size controls and details.

`--render-owner-visibility` exports copied-map visibility, local no-selection behavior, supplied sensor/range fixtures and minimum-size controls/details.

`--render-visible-candidates` exports the joined child entry, natural owner results, retained-history candidates and minimum-size controls/details.

`--render-visible-preparation` exports the candidate continuation entry, original-equipment preparation, before/at-deadline states and minimum-size controls/details. Firing remains guarded.

`--render-visible-requests` exports the joined request entry, original F2/F3 request cells, global-field/full-queue diagnostics and minimum-size controls/details. No shot is executed.

`--render-produced-launch` exports the natural-distance consumer entry, held timers, F2 range denial, F3 allocation/capacity cases and minimum-size controls/details.

## Repository layout

- `src/`: canonical maintained C# source.
- `scripts/`: asset manifests and preparation/extraction scripts.
- `Setup.ps1`, `Build.ps1`, `Launch.cmd`: reproducible setup, build and launch.
- `build/`: ignored generated runtime and local settings.
- `work/`: ignored analysis recipes, native evidence and intermediate files.
- `outputs/`: ignored local guides, reports and checked recovery archives.

Original media, binaries, user settings, historical builds and research artifacts are excluded from Git. If present, `outputs/Current-Status.txt` and the latest `work/checkpoint-*-handoff.txt` provide private continuity; a fresh checkout builds without them. Local maintenance keeps the current runtime and one previous comparison build, preserving user data and checked recovery paths when retiring temporary copies.
