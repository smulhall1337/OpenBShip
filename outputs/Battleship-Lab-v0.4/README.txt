Battleship Fleet Lab 0.4 — recovered Classic opponent targeting

Run BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Choose an opponent setting before deployment, then Auto-place -> Start battle.
Click the upper-right board to fire. P pauses/resumes; F2 starts fresh.

New in v0.4:
- Original target 1..4 use the recovered Classic target selector and original
  integer random-generator algorithm. It pursues hits along the native fixed
  ship axis, then performs a search using the selected attempt budget.
- The four settings map to native values 0..3 and budgets 10/50/100/200. The
  labels are new neutral names; no original difficulty names or calibrated
  strength ranking are claimed. Original target 1 is the default, matching
  the original default configuration value.
- Prototype pursuit retains the earlier random/adjacent-cell opponent for
  comparison. The rotating v0.1 fleet uses that strategy automatically.
- Changing targeting resets deployment. Choices are locked during battle and
  retained across F2 and fleet-preset changes. They reset on application restart.
- Original search exhaustion can repeat an already resolved square. This is
  preserved: it consumes the opponent turn, causes no extra damage, and appears
  in the log as "repeat / no new damage". Player repeats remain rejected.

Targeting evidence is unusually direct: an isolated x86 analysis tool executes
copied original numerical selector/RNG code against controlled observations.
The new C# selector matches its coordinate AND final RNG state in all 113 captured
cases, covering hit pursuit, edges, all six search-pattern branches, difficulty
budgets, masked intact ships and exhausted/repeated choices. This is numerical
subsystem validation, not a claim that the complete original game runs faithfully.
Read OPPONENT-EVIDENCE.txt for addresses, fixture provenance and exact limits.

Retained play:
Recovered Classic has Super Carrier 5, Battleship 5, Frigate 3, Missile Submarine 4,
Destroyer 2: 19 cells, original fixed axis, touching allowed, no overlap or off-board
ships. Manual deployment centers the ship on the clicked lower-left cell. Original
ship art, native board coordinates, translucent placement preview, fleet-status
icons and HIT/MISS artwork remain. The v0.1 preset keeps its rotating 17-cell fleet
and earlier mapping. Earlier v0.1/v0.2/v0.3 packages are preserved; parent launchers
Launch-Reconstruction-v0.1.cmd, -v0.2.cmd and -v0.3.cmd open them.

Controls:
Mouse places/fires. Arrow keys move the grid cursor; Enter places/fires. R rotates
only in the v0.1 preset. P/Escape pauses/resumes, F2 resets, Sound toggles effects,
Quit exits. Focus loss pauses. Resume explicitly afterward. Sound preference is
saved under UserData/settings.txt. Existing bindings are retained. Selectors use
normal keyboard navigation while focused. Recovered maps is a read-only tab; it
pauses battles and does not execute the three decoded scenario missions.

Reconstruction versus modernization:
The target-choice algorithm, search budgets, observation masking and RNG arithmetic
are recovered. Its RNG stream is isolated in this port: native layout, weapon,
shooter-selection and movie calls also consume the original shared generator, so
whole-game shot sequences are NOT claimed identical. Random layout generation,
first-player choice, response timing, weapon presentation, pause/reset, hit-cell
markers and surrounding controls remain modern/provisional. The original movies,
fire animations, real-time fleet simulation, missions, multiplayer, saves and
controller input remain absent. Complete native palette state is unresolved; the
accepted PCX palette appearance is retained. Native inverse x87 boundary ties
retain the precision limitation recorded in PRESENTATION-EVIDENCE.txt.

A deliberate finite failure replaces one original infinite-loop case: an isolated
known hit with no open Y neighbor pauses with a clear F2 reset message. Direction
retries also have a finite diagnostic limit. This trap was not encountered in the
valid fixed-axis battle checks. No silent fallback chooses a different target.
Original targeting is disabled for the rotating fleet, which violates its axis
assumptions. Persistent result banners and post-battle reveal remain modern UI.

Validation:
32 focused checks and 7 real-form checks pass. The 113 oracle comparisons are
numerical cases inside one check. The 400 board centers, 128 layout seeds per
preset and 15 additional complete-battle runs are grouped sweeps, not hundreds of
separate gameplay test claims. Form checks simulate input through actual handlers,
including minimum-size layouts, preset/target choice reset/locking and F2. Six
packaged-form previews are visually reviewed at normal/minimum sizes. These do
not establish physical input/focus, audible output or full original-game fidelity.

Build.cmd rebuilds the Windows x64 C#/.NET Framework Forms/GDI+ game with the
existing Windows compiler. Verify.cmd runs its model/reader and form checks.
Source modules separate model, original targeting/RNG, geometry, ship presentation,
readers, rendering, audio and UI. No new engine/runtime is installed. The game
loads relative bundled assets and does not need the ISO, original registry keys,
graphics wrapper or the optional x86 oracle/disassembler tools.

Diagnostic commands:
BattleshipLab.exe --self-check Self-Check.txt
BattleshipLab.exe --ui-check UI-Check.txt
BattleshipLab.exe --render-preview Preview.png
BattleshipLab.exe --render-fleet-preview Fleet-Preview.png
BattleshipLab.exe --render-battle-preview Battle-Preview.png
BattleshipLab.exe --render-small-preview Small-Preview.png
BattleshipLab.exe --render-legacy-preview Legacy-Preview.png
BattleshipLab.exe --render-map-preview Map-Preview.png

Twelve original assets are hash-identical to the supplied RIP. Asset/dependency/
build manifests, raw evidence, source and check reports are bundled. Tools/README
explains the optional hash-guarded analysis tools. The accepted original launcher
remains Launch-Original.cmd. Earlier user "looks good" feedback is recorded as
appearance acceptance; a full battle or audible sound is not inferred from it.

Next hands-on check: compare Original target 1 with Prototype pursuit over several
turns, then try another original setting. Watch the shot log for line pursuit or
repeat/no-new-damage results. Next static work is complete native palette state
and combat feedback timing, keeping this targeting/art checkpoint for comparison.
