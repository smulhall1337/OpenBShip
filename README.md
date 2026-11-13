# OpenBShip

A partial C# reconstruction of **Battleship: The Classic Naval Warfare Game (1997)** for modern Windows x64. The current game source is v0.57.

## Requirements

- Windows x64 with the .NET Framework 4.x compiler at `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`.
- An extracted copy of the supported English game. The folder must contain `Bs.exe`, `GRAPHICS`, `LWEBS`, `HWEBS` and `SFX`.

Original game assets and executables are **not included**. Setup checks the supplied files against the current asset manifest and extracts the small runtime programs, original cursor resources and seven unit/object-data tables from `Bs.exe`. The supported executable has SHA-256 `B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569`.

The supplied RIP and disc installer contain different executable builds. The supported RIP `Bs.exe` is 397,824 bytes; the installer cabinet's `Bs.exe` is 401,408 bytes with SHA-256 `AFB5D842D4DF17D3957EA31560D07113AD61452AFD2F1E7A9DC232E2D2C874B4`. Their code and data layouts differ. The installer also contains a separate `BShip.Exe`. These builds are preserved as separate research inputs; setup and the current reconstruction evidence support only the pinned RIP build above.

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

Generated files belong in ignored `build/`. Only `build/current` is required to play. Preserve `build/current/UserData` when cleaning or moving the workspace: it holds your settings and saved battles. Old packaged builds and temporary verification workspaces are optional; they are not launch dependencies. Verified temporary copies are retired after their source, settings and evidence have a checked recovery path; the local workspace keeps the current build and one previous comparison build.

`Launch.cmd` waits for a key if setup or required assets are missing. Other startup failures show an error dialog and write details to `build\current\UserData\last-error.txt`.

The optional software comparison export uses the reconstructed original grid, cells, hull/smoke and HUD renderer:

```powershell
Start-Process .\build\current\BattleshipLab.exe -ArgumentList '--render-software-preview', '.\build\software-preview.png' -Wait
```

Run this from the repository folder after setup. It writes a 640×480 PNG of the verified seed-0 idle scene. Use `--render-software-effects-preview` with the same output argument to export the verified first-miss scene, including its original ring entry. Use `--render-software-text-preview` to export the verified idle scene with original sprite-font text. Use `--render-software-movie-preview` or `--render-software-high-movie-preview` for a verified F18 movie/world/text composition in the two original PAT formats. Use `--render-software-transition-preview` for a prepared idle scene with the original result-transition HUD gate applied; hulls and the background remain visible. The battle PNG exports use the documented shared gameplay palette. Use `--render-software-title-preview` with the same output argument for the verified title scene and selected Solo button. The title export uses its original card palette with the documented modern reserved Windows colors; the normal menu shares this renderer. These are software comparison fixtures; normal play retains its current cinematic presentation and controls.
To export all 21 original setup descriptions with the recovered line wrapping:

```powershell
Start-Process .\build\current\BattleshipLab.exe -ArgumentList '--export-practice-text', '.\build\practice-text.txt' -Wait
```

The export includes the input hash and original formatted rows. The playable Practice screen renders those rows with the recovered sprite font.

To export the original Practice setup screen with its sprite-font descriptions:

```powershell
Start-Process .\build\current\BattleshipLab.exe -ArgumentList '--render-software-practice-preview', '.\build\practice-preview.png' -Wait
```

This static 640×480 comparison uses the original card, selected options, description, scroll arrows and page counters. Its renderer matches 150 prepared original scenes, and its six font styles match 90 original glyph-rendering cases. The palette uses the documented modern reserved Windows colors. These checks establish the supplied scene states; these supplied scenes do not establish the complete Practice interpreter or physical input behavior. The playable Practice screen shares this renderer. Its keyboard navigation, matched-release mouse activation, focus pause and 40 ms fades remain deliberate modern choices.

Use `--render-software-practice-scroll-preview` with an output PNG argument to inspect page 2 of a longer original description and its 2/3 counter. This explicit comparison uses description 14; normal Practice selections keep their original descriptions. The screen also exposes its current description as accessible text.

The Fleet Configuration software comparison exports use the same renderer as the playable setup screen:

```powershell
Start-Process .\build\current\BattleshipLab.exe -ArgumentList '--render-software-fleet-preview', '.\build\fleet-preview.png' -Wait
```

Use `--render-software-fleet-hover-preview` with an output PNG argument for its original Start Game hover sprite. The fixed seed-0 comparison includes the original main grid, miniature fleet, selection border, inactive slots, full-size hulls and disabled Classic purchase overlay. Original code and the maintained renderer agree across all 70 recorded layouts in normal, Start-hover and Back-hover states. A separate 808-poll study checks the source hit boundaries and full scenes. The playable control retains the modern gold cursor, atomic placement previews, keyboard support and matched-release activation.

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

For faster debugging, enable **Options → Debug → Skip turn cinematics (debug)**. Both sides' shots resolve without loading turn videos, while preserving damage, turn order and random choices. You can change it during a battle; a paused shot resolves when you resume. The setting persists locally and defaults off. Intro videos, result sequences and the clip library remain available. Turn it off to restore normal turn playback.

**Recovered maps** exposes all twenty-three grouped fleet records in four imported maps. Choose a unit in its roster or schematic grid to inspect its name, local hull placement, hit points, source speed, equipment selections and ammunition. Long details can be scrolled and copied. The default record view retains its explicit kind-2 constructor fixture.

Enable **Seeded deployment** to place the grouped fleets using the recovered owner-tagged map zones, occupied-origin rejection, original random generator and player/group/record order. Enter a seed from 0 to 4294967295; repeating it reproduces the preview. Click a numbered marker or its label to select that fleet. Nearby labels use leader lines; the origin itself stays at its original coordinate. The inspector shows the grouped kind (1 or 9) and world origin. Previewing maps preserves the active Classic battle and settings.

For the observed first Practice map, select **BSSB201.SCN → Seeded deployment → Include map objects**. This also reconstructs the original pool IDs and default fields, seven kind-7/type-25 records and their single-unit local placement before creating the six grouped fleets: thirteen fleets and nineteen nonzero ship records in total. Seed **143514006** reproduces one observed startup data state, including its 548 random calls. The initial RNG state is supplied; application seed selection is not reconstructed. Object labels retain their numeric types where their full meaning is unresolved.

Choose a **player 1 grouped fleet** in that first Practice startup preview, then open **Waypoint study…**. Click a map cell or enter world X/Y (0–639) and use **Add / select point**. The recovered branch snaps points to multiples of ten, rejects terrain kind 2 for these fleets and supports five slots. Clicking an existing point selects it; **Deselect point** allows another append. With map focus, arrows move the cursor and Enter adds a point.

Use **Step fleet** for one recovered movement update, or **10 updates** for up to ten. A batch stops early when the queue completes or movement is blocked. The gold marker and origin readout follow the selected fleet; the other fleets stay fixed. Fixed fleet dots show their identity on hover, and a blocking fleet is highlighted. On a blocked route, use **Clear queue** to plan a detour. **Reset study** restores the starting position, queue and auxiliary state. Closing the study preserves the original deployment, Classic battle and settings.

**1 source poll** and **32 source polls** advance the recovered normal batch cursor: two batches per poll, four fleet slots per batch, with a final two-slot batch. The selected fleet moves only when its slot is visited. The phase readout shows the next batch and completed cycles. The existing update buttons leave that cursor unchanged. Clear queue preserves position and phase; Reset study restores both. The controls scroll vertically in smaller windows.

This is a copied-state movement study with schematic terrain. One update is not an original frame, and source polls are explicit steps with no wall-clock pacing. The poll buttons now execute complete admitted batches in source order. Only the selected fleet receives queued orders in this study, so the other fleets remain idle and fixed. The study supplies zero auxiliary data and an inactive secondary pool. Active secondary objects, AI, pathfinding, point dragging/editing and mission execution remain unfinished. Unsupported modes or interactions report a limit while preserving that update. The study interface, reset and deselect controls are deliberate modern choices.
The map inspector remains a read-only startup diagnostic. Placement matches 21 original contexts and 63 accepted origins; grouped initialization matches complete pools, consumed records, counters and random histories across nine seeded runs containing 51 fleets. It starts with fresh pools and a supplied seed. The grouped-only view excludes earlier startup and map objects. The first Practice option covers the verified ship/fleet pipeline and 54 blockers in the original 500-record primary terrain pool. Other terrain buffers, application setup, local-player UI and mission simulation remain unfinished. The original placement search is unbounded; the maintained adapter reports exhausted zones or search limits and preserves the failed search state. Rerun Setup after updating an older prepared folder to import the seven unit/object-data tables.

To keep a duel between app sessions, use **Options → Save battle checkpoint** when it is your turn and all shot movies have finished. This supports Recovered Classic with any of the four original opponent settings. Saving pauses the game. **Load saved battle…** validates the file before asking to replace your current game, then restores it paused; press P or Resume to play. **Load previous checkpoint…** recovers the file retained before the most recent save.

The modern checkpoint lives at `build\current\UserData\battle.obship`, with one `.bak` copy. It preserves the actual starting fleets, accepted shots, selected ship, difficulty, movie-turn mode and random-stream history. Replay validation rejects damaged or incompatible files without replacing the current game. Display/audio preferences stay as currently configured. Pending shots, finished battles and the prototype opponent/rules are unsupported. Original game save files are not compatible.

Fleet Configuration now shares its complete original software scene with the comparison export: main grid, miniature ships, selection border, inactive-slot art, full-size hulls and disabled Classic controls. Start Game and Back display their original hover sprites. It follows committed moves and shuffles. A selected ship stays visible during a placement preview, and Escape cancels that preview. Starting the battle fades that completed fleet scene to black, shows the loading card, fades the card out and reveals the battle board. Pause and Return to fleet remain available until the board appears. Targeting unlocks after its reveal; focus or tab loss pauses the transition, and P resumes. These phases use the original 17 exit and 14 entry factors. The 40 ms cadence and RGB scene dimming are explicit modern presentation choices.

The Classic board uses the original targeting and hand cursor art. Untried enemy cells show a target, selectable friendly ships show a hand, and a shot or opponent turn shows a wait cursor over the board. Pause, resolved cells and finished battles use the arrow. These hover rules are an intentional usability policy; the shapes and target/wait transitions come from the original resources and code. Older prepared folders fall back to system cursors; rerun Setup to extract the original shapes.

Miss rings and the five original sinking clips now appear when a Classic shot resolves. **Animated damage effects** controls these transitions; turning it off clears active clips. They freeze during pause, and reset/restart clears them. The full-detail ship sprites use original frames and placement independently of the movie-detail setting. Rerun Setup after updating to import these resources.

## Reconstruction status

Original intro replay, Credits, Quit, victory/defeat sequences and the movie archive are available. Cinematics play behind the board with the accepted 30% board-surface transparency. Campaigns, networking, tutorials, radar, controllers and the complete original lifecycle remain incomplete.

Turn movies and damage effects retain the accepted modern 40 ms step. A read-only study of the supported original executable on the current compatibility setup measured about 50 main-loop counter increments per second: its 40 ms wait uses the previous iteration's clock value. This corrects an earlier 25 Hz inference; it does not establish a universal historical frame rate or audiovisual synchronization. Production pacing remains unchanged pending a fuller playback comparison.

The project keeps two tracks: reconstruct observable source behavior, and make deliberate usability changes. The current build is a playable Classic slice, not a complete port of every original mode.

| Area | Evidence and current boundary |
| --- | --- |
| Containers and graphics | Bounded readers recover the supported PCX, sprite/font banks, palettes, PAT movies, sound sequences and executable programs. Full-buffer comparisons cover original menu, Fleet and Classic world drawing, including text, hulls, smoke, effects, HUD and movie composition. |
| Scenario formats | The original reader, an independent model and the maintained reader agree on complete sparse buffers, opaque payloads and player-group data for 120 supplied scenario files. Three maps also match with two diagnostic buffer fills and exact repeat captures. Eight boundary cases and five altered-evidence checks pass. One additional file fails bounded admission and remains unsupported. The lab imports four inspection maps; these format checks do not implement mission simulation. |
| First Practice startup | Two observed original WinMain runs load `BSSB201.SCN` for type 1 with two players. Before grouped creation there are seven kind-7 map cells represented as kind-11 fleets with type-25 records. Six grouped fleets add twelve units, yielding thirteen fleets and nineteen nonzero ship records. Independent models match the complete pre/post grouped pools, consumed map data, counters and random states. An isolated source pipeline now recreates both observed ship/fleet states directly from the SCN and captured starting RNG state, including pool defaults and type-25 construction. An independent model agrees across five seeds. Other terrain buffers, application startup, local UI and mission simulation remain separate; the lab now includes that data pipeline as the optional first Practice startup preview. |
| Practice waypoint data | Two actual original runs supply fifteen same-frame input pairs, including four exact waypoint writes; later observations show movement and queue consumption. An isolated append/duplicate branch matches independent and maintained models across 33 prepared contexts, with exact repeats of complete pools. Four original clear-queue contexts also match complete pools and preserve Y/opaque bytes. The maintained study adds real form/button/map/keyboard checks and copied-state preservation. Original right-button camera drag, movement, collision, point editing, network and mission behavior remain separate. |
| Practice movement boundary | Twenty atomic updates from one owned original session (15 cardinal, five diagonal, one arrival) match an isolated original movement segment and an independent numeric/queue model across whole ship/fleet pools, auxiliary data and captured globals. Exact repeats and six altered-evidence guards pass. This admits kind 1, observed flag 2, clear movement, state 0 and network disabled. It recovers the source speed factor, float stores, signed rounding, facing and queue consumption for those updates. Other flags, collisions, scheduling, wall-clock pace and complete mission behavior remain unfinished; the maintained study exposes the verified branch through manual updates. |
| Prepared movement cases | A separate 103-context source matrix matches an independent model for two flags, four speeds, direction/saturation cases, fleet/terrain collision edges and arrival state. Five fleet blocks, two terrain blocks and 27 arrivals are covered. Complete pools repeat exactly and six altered-evidence guards pass. These prepared boundary inputs exclude kind-12 destruction, other modes and the full scheduler. Numeric flag meanings and physical units remain unresolved. |
| Practice terrain startup | The original SCN terrain/special-object scan now executes as a bounded source pipeline. Across five seeds it preserves 70 earlier ship/fleet/map/random files exactly. An independent model and an original-process capture agree on all 500 terrain records, including 54 active blockers, source scan order and slot IDs. Native repeats match and seven altered-evidence checks pass. The separate 125-record secondary pool is empty for this pinned map; other terrain kinds and a complete world startup remain unfinished. The packaged study now exposes bounded movement updates using this primary terrain pool. |
| Maintained movement module | `PracticeTerrainPool` and `PracticeMovementState` now reconstruct the first-map blockers and an admitted kind-1 fleet update in C#. Complete fleet/auxiliary/terrain buffers and waypoint selection match 169 prepared original contexts and 20 atomic original-process updates. Seventeen malformed/unsupported-state checks cover ownership and atomic failure; an unsupported kind-12 collision preserves the input state. This is a single-fleet branch with fixed blockers and supplied auxiliary context, excluding AI, network and mission execution. The packaged study exposes explicit updates and selected-fleet poll composition with clear/reset and fixed blockers. |
| Coupled movement histories | Twelve prepared source histories containing 1,680 updates now match the maintained movement and waypoint modules at every full-buffer hash and terminal state. They cover 42 waypoint consumptions, duplicate selection while moving, clear/append rerouting after fleet and terrain blocks, retained arrival state, another selected fleet and idle updates. Native repeats are exact and eight altered-evidence checks pass. These are twelve bounded data histories with recorded UI endpoints, excluding original scheduling, AI, wall-clock pacing and mission execution. |
| Practice batch enumeration | The maintained scheduler matches 74 controlled original-code contexts: four-slot batches, the final two slots, 63 batches per cycle, counter wrap and the caller's two-or-65-batch branch. Two owned original sessions supply 256 paired normal-mode calls in 128 complete two-call polls. Native repeats and eight altered-evidence checks pass. Per-slot movement and secondary-pool work were replaced by recorders in this scheduler probe. The original S toggle displays FAST/SLOW; an observed same-poll renderer cancellation prevents a sustained fast-mode comparison. Wall-clock pacing remains unverified. |
| Selected-fleet poll study | v0.56 composes normal batch enumeration with the separately verified movement branch. Twelve source histories still match all 1,680 selected-fleet updates, with the expected alternating 32/31-poll intervals. Real form checks cover one/32-poll controls, idle polls, manual-step phase preservation, clear/reset, queue completion, terrain blocking and a scrollable small-window layout. Other fleets remain fixed; this composition does not execute the original full-world update. |
| Whole original movement batch | Across 169 prepared contexts, the complete original batch function now builds its own local frame, visits three inactive slots and the moving fleet, runs its empty 125-record secondary-pool update and returns. Complete buffers and normalized globals match the independently checked movement cases plus the inactive-slot preamble. Native repeats are exact and nine altered-evidence checks pass. This removes the supplied movement-stack fixture. Twenty earlier actual movement contexts also replay through the complete batch using their observed map columns and random state; replay of preceding stationary records and a supplied empty secondary pool remain explicit limits. Caller AI, rendering/input, network and wall-clock pacing remain excluded. |
| Actual whole-batch boundary | A fresh original-game scenario supplies twenty atomic batch entry/return pairs, including the complete secondary pool, map columns and caller/frame controls. Complete original-function replays match every captured pool and global; the maintained movement body and cursor also match. Nineteen consecutive intervals follow the predicted 32/31-poll cadence. Exact repeats and eighteen altered native/observation checks pass. These twenty batches belong to one scenario; caller AI, input/render dispatch, active secondary objects and wall-clock pacing remain separate. |
| Whole admitted poll module | v0.57 uses a maintained batch state for the poll buttons in the study. It matches 23 prepared histories across five recovered startup seeds: 2,530 polls, with whole-pool hashes and terminal buffers agreeing with the original caller and batch functions. Checks cover two moving fleets, order-dependent lock clearing on arrival, sequential collisions, cycle wrap and early-skip kinds. It also matches 169 prepared and twenty fresh actual whole batches. Unsupported record modes, nonzero field476 states and active secondary objects fail atomically. AI, active secondary interactions and elapsed-time pacing remain absent. |
| Classic gameplay | Original fleet creation, placement, selection, targeting, damage, sinking, opponent decisions and terminal callbacks are compared with independent maintained models. Original addresses and fixtures are tied to the supported executable hash. |
| Continuous source execution | Sixteen controlled sessions connect the initial menu, Fleet setup, complete battles with original media, endings and returned menus. Independent comparisons cover 299,056 battle scenes, 11,608 ending ticks and 2,288 returned-menu ticks. These are trace records within sixteen sessions, not hundreds of thousands of independent gameplay scenarios. |
| Second game | Four carried win/loss and difficulty contexts extend the source chain through a second Classic setup and world readiness. Full ship, resource, AI, random-state, camera, palette and scene models agree. Gameplay beyond that boundary is blocked by the carried original turn phase described below. |
| Actual original reference | Separate real processes complete one victory and one defeat through normal endings and menu returns using the accepted compatibility setup. Observation uses read-only memory access and ordinary window messages. This is distinct from the controlled interpreter/renderer studies and from physical input/audio acceptance. |
| Modern usability | Keyboard/mouse navigation, pause/focus handling, cinematic presentation, deterministic restart, validated battle checkpoints, persistent settings and the debug movie bypass are intentional modern behavior. Turn movies appear behind the board with the accepted 30% surface transparency. |
| Still incomplete | Non-Classic campaign/mission behavior, complete tutorials and scripts, networking, radar, native controller support, complete original application/device lifecycle and equivalence across other executable editions. Practice descriptions and scene previews do not establish playable original missions. |

In controlled experiments, file, heap, numeric input, clock and audiovisual device boundaries are supplied or recorded. Matching a decoder, scene or script boundary does not establish the entire original application. The full original shared random stream and wall-clock audiovisual synchronization remain separate work; the playable build retains distinct gameplay and presentation random streams.

A bounded follow-up also matches the original enumeration of seventeen 400-byte fleet records: 65 ordered unit declarations and every mutated payload byte agree with an independent model, with exact repeats and five altered-evidence checks. Positioning, reset, unit factories and finalizers are recording endpoints in that study. It establishes record enumeration rather than original fleet allocation, mission initialization or simulation.

That record study now also executes the original allocator, template constructor, ammunition setup, weapon selection and fleet color/speed/occupancy finalizers. Independent models agree on all 1,500 ship slots and 250 normalized fleet records for the same seventeen inputs and 65 created units. Full captures repeat exactly, and six changed-evidence cases are rejected. Position remains supplied; the local-player UI branch is excluded by an explicit fixture. Natural placement, mission initialization and simulation remain separate work.

Twenty-four additional controlled cases check fourteen unit templates and allocator boundaries: neighboring reserved types, inactive roster IDs, live references, fleet-kind distinctions, the final slot and exhaustion. Complete pools repeat exactly and five altered-evidence cases are rejected. The extra ammunition index in one template preserves an adjacent nonzero field through its original zero-addition; the model retains that behavior without inventing an equipment meaning.

A separate real original process also reaches the default two-player Practice type 1 fleet setup and its non-Classic world view through normal menus, then closes cleanly. Read-only live snapshots and a software display capture record that entry boundary. Mission controls, completion and equivalent behavior in the rebuild remain untested.

The supplied original has a reproducible second-game limitation on this setup. After nineteen aimed hits produce a victory, or sixty-nine water-only shots produce a defeat, normal menus reach a second Classic board. Three released target presses reach the native input latch and correct world cell across advancing frames, but turn phase 3 after victory or 7 after defeat remains carried; damage and random state do not advance. Independent checks validate both observation policies and transitions, and nine altered evidence cases are rejected. This applies to the supported RIP and accepted cnc-ddraw profile at difficulty 0. Behavior of the distinct disc executables remains untested. The rebuild initializes a fresh battle and does not reproduce this stall.

The original's default human roster name also appends across the observed second setup, and controlled AI startup scales carried weights again. Both are recorded as source behavior without imposing them on modern play. A decompiler listing or a running reference process is evidence, not recovered original source or a finished port.

## Validation and private research

The packaged v0.57 passes 190 model/format groups and 119 real-form groups (309 total), 56 shutdown/lifecycle cases and silent Windows waveOut API checks. The earlier seventeen-record view and 51 grouped preview selections still pass their original comparisons. The first Practice startup view adds 65 origin/label selections across five source contexts, with complete pools and selected records matching the original data, without replacing the active battle. Dedicated checks exercise the actual Debug menu, persistence across three processes, cancellation of active silent output, pause and callback behavior. Twelve paired rounds across the four original opponent settings preserve the same logical state with and without turn playback. A complete debug duel then plays its result movie, returns through the menus, starts a second battle, resolves its first shot and restores normal cinematic playback.

The current source-only Git export also passes public setup, compilation and a visible launch from a different working directory before any private fixtures are attached. Its Debug menu persistence and complete two-battle flow pass separately. Silent samples establish output API handling, not audible confirmation; automated form input does not establish physical controller behavior.

Private research records include executable identities, original-format analysis, confidence limits, numerical traces, full-scene captures, independent checkers and rejected evidence mutations. Immutable archives have been stream-hashed and selected source studies rebuilt from fresh restores. The latest restore rebuilds seven native profile families and reproduces all 3,855 recorded output files exactly, alongside maintained-model and real-form checks from a fresh public build. These local archives and the optional `build/current/Evidence` fixtures are not dependencies for building or playing a fresh checkout. Detailed historical comparisons remain in local handoffs and Git history rather than being treated as current user-facing behavior.

AI upscaling of original clips remains an optional stretch goal. It has not been implemented; preserve the original media baseline and assess cost, artifacts, temporal consistency and audiovisual synchronization before adopting generated replacements.

## Repository layout

- `src/`: canonical current C# source and in-source diagnostics.
- `scripts/`: asset/program manifests and import/extraction scripts needed by setup.
- `Setup.ps1`, `Build.ps1`, `Launch.cmd`: setup, compilation and launch entry points.
- `build/`: ignored generated game, local settings/saves and optional comparison fixtures.
- `outputs/`, `work/`: ignored private reports, recovery archives, tools and intermediate research.

Original assets, executables, settings, generated binaries and historical packages stay out of Git. Keep `build/current/UserData` when cleaning the workspace. The current source and scripts plus an explicitly supplied supported original are sufficient to reproduce the playable build.
