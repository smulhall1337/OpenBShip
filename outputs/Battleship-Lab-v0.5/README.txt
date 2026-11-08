Battleship Fleet Lab 0.5 — original damage effects and display choices

Run BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Choose Auto-place -> Start battle; click the upper-right board to fire.
P pauses/resumes, F2 starts fresh. Earlier v0.4 and other builds are preserved.

New in v0.5:
- A hit on a surviving player ship now draws its original Classic damage effect.
  SMOKE.SPH supplies 33 frames, with recovered per-segment phase, 79% scaling and
  bottom-centered offsets. It loops until that ship sinks. Opponent positions
  remain hidden; these effects are drawn only on the player fleet.
- The new Display button offers Colors: v0.4 appearance (default), Colors:
  recovered gameplay, and Animated damage effects. Uncheck animation to freeze
  the current effect frame. Display changes pause an active battle; use Resume/P
  afterward. They preserve the battle and targeting state.
- Color and motion preferences persist alongside the existing sound preference.
  Old sound=false files still mute correctly; unknown key/value pairs are retained.
  Settings are written atomically under UserData/settings.txt.

The recovered gameplay profile uses ISLAND.COL plus the 15 original loader patches
for entries 10..245. The native API preserves 20 Windows palette endpoints; v0.5
uses deterministic endpoint colors observed in a local probe. This is a documented
modern boundary, not a capture of a 1997 desktop's exact palette state. The earlier
PCX palette remains the default. DISPLAY-EVIDENCE.txt describes the full evidence
and the reserved-color limitation.

Original palette-patch and fade arithmetic was checked against isolated execution
of the original numerical code. All 65 tested fade factors match byte for byte.
Automatic startup/exit fades are not enabled: their original presentation cadence
has not been established. The damage clock targets 25 Hz from the source's 40 ms
Classic-loop limiter, with bounded visual catch-up. Pausing, focus loss, the map
inspector and still mode freeze it. A new game resets its phase. Those pause/reset
choices are intentional modern behavior; exact original global frame phase and
performance-dependent timing are not claimed identical.

Retained play:
Recovered Classic has 19 occupied cells: Super Carrier 5, Battleship 5, Frigate 3,
Missile Submarine 4, Destroyer 2. Place ships by their center on the lower-left
board along the recovered fixed axis, or Auto-place. No overlaps or off-board
ships; touching is allowed. Each valid shot ends the turn. Original target 1..4
use the recovered target selector and search budgets; Prototype pursuit retains
the previous strategy. Targeting changes reset deployment, are locked in battle
and survive F2/preset changes. They reset on app restart. Original exhausted search
can repeat a square; it consumes the turn with no new damage and is logged clearly.
The rotating v0.1 preset retains its prototype strategy and older mapping.

Controls:
Mouse places/fires. Arrows move the grid cursor; Enter places/fires. R rotates in
the v0.1 preset only. P/Escape pauses/resumes, F2 resets, Sound mutes/unmutes, Quit
exits. Display is keyboard accessible when focused. Its open menu owns confirmation
keys; held confirmation is suppressed after closing until release. Focus loss
pauses battle; resume explicitly. Recovered maps remains read-only and pauses a
duel. It does not execute the decoded original scenario missions.

Evidence and scope:
DISPLAY-EVIDENCE.txt covers the new palette and damage paths. OPPONENT-EVIDENCE,
PRESENTATION-EVIDENCE, CLASSIC-EVIDENCE and FORMATS retain the current targeting,
geometry, rule and format findings. Source addresses and raw byte fixtures are
bound to the exact supplied Bs.exe SHA-256. Thirteen packaged original assets are
byte-identical to the supplied RIP. Originals and earlier builds are unchanged.

This is a partial reconstruction. Native target-choice arithmetic is recovered,
but its RNG stream is isolated from the original shared layout/weapon/movie RNG.
Layouts, first-player choice, 600 ms opponent delay, pause/reset, alpha preview,
modern hit markers, persistent result banners and post-battle reveal remain new
behavior. The source inverse's x87 boundary precision limitation remains. Original
weapon/shooter selection, combat movies, transient weapon/explosion sequences,
full palette transition scheduling, missions, real-time fleet simulation,
networking, saves and controller support remain absent or provisional.
Physical input/focus, audible effects and complete original-gameplay comparison
remain separate hands-on checks. No complete/exact port claim is made.

Build and checks:
Build.cmd uses the existing Windows .NET Framework compiler for a Windows x64
Forms/GDI+ game. Source separates readers, model, targeting, geometry, ship and
damage presentation, palette data, preferences, audio and UI. The game needs its
bundled assets; it does not invoke the original executable, mounted ISO, original
registry keys, graphics wrapper or optional x86 analysis tools.

Verify.cmd runs 38 focused checks and 9 form checks. Numerical sweeps (113 targeting
oracle cases, 65 palette fade factors, 400 board centers, layout seeds and grouped
battle runs) are cases inside checks, not hundreds of independent gameplay claims.
New form checks exercise palette switching, old-image disposal, keyboard routing,
battle-state retention and the live visual timer through pause/still/reset. These
are offscreen application tests with simulated input, not physical desktop input.

Diagnostic previews use a fixed comparison seed:
BattleshipLab.exe --render-preview Preview.png
BattleshipLab.exe --render-fleet-preview Fleet-Preview.png
BattleshipLab.exe --render-battle-preview Battle-Preview.png
BattleshipLab.exe --render-small-preview Small-Preview.png
BattleshipLab.exe --render-legacy-preview Legacy-Preview.png
BattleshipLab.exe --render-map-preview Map-Preview.png
BattleshipLab.exe --render-damage-preview Damage-Preview.png
BattleshipLab.exe --render-recovered-colors-preview Recovered-Colors.png
The last two deliberately inject one known player-ship hit for a controlled visual
comparison; the log/status labels it as a damage preview. They use the same scene
and animation phase, changing only the selected palette. This diagnostic injection
is not a normal gameplay action or evidence of an original battle.

The package includes source, manifests, integrity/portable/tool/oracle reports and
previews. Tools/README explains the optional read-only extractors and numerical
oracles. Parent launchers -v0.4.cmd/-v0.3.cmd/-v0.2.cmd/-v0.1.cmd retain prior builds;
Launch-Original.cmd remains the accepted original menu reference.

Next hands-on check: take a hit, inspect the animated damage, open Display and
compare colors or freeze the effect, then Resume/P. Next static work: original
weapon/shooter selection and transient shot feedback; keep this checkpoint as the
persistent-damage and palette-data baseline.
