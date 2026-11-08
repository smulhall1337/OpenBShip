Battleship Fleet Lab 0.6 — firing ships and recovered weapon choices

Run BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Auto-place -> Start battle. Select a live ship in the right-hand fleet list,
on your lower-left board, or in the original fleet icons. Its cells/icon gain
a gold outline. Fire on the upper-right board and read the shooter/weapon/result
in the log. Weapon choice is automatic, as in the traced Classic code.

New in v0.6:
- Both fleets begin with Battleship selected. A selected ship's sinking moves
  selection to the first surviving roster slot. Sunk ships cannot fire.
- Player carrier, submarine and other ships use their recovered weighted lists.
  The opponent retains its separate generic list, irrespective of ship type.
- Original weapon names appear in the log. Each shot still affects one cell;
  these names do not imply new ammunition, damage or area-of-effect rules.
- Intended original firing-clip IDs are recovered and retained in the model for
  analysis. Firing movies are NOT played in this checkpoint.

Quick comparison:
Select Super Carrier and fire: its choices are Multi Air Rockets/Multi Air
Missiles. Select Missile Submarine on your next turn: its list is Large Torpedos,
Submarine Cruise Missile, Torpedos. Other Classic ships use the generic gun/
torpedo/missile list. Selection itself consumes no turn and fires no shot.
The fleet list supports Up/Down/Home/End and skips sunk ships. Confirmation while
that list is focused does not fire into the grid. Click the board to restore grid
keyboard focus. Arrows + Enter still fire there; R rotates only the v0.1 preset.
P/Escape pauses/resumes, F2 resets, Sound mutes/unmutes, Quit exits. Focus loss
pauses; resume explicitly. Settings changes during deployment reset the fleet.

Retained play:
Recovered Classic uses 19 cells: Super Carrier 5, Battleship 5, Frigate 3, Missile
Submarine 4, Destroyer 2. Place ships by their centers along the fixed original
axis, or Auto-place. No overlap/off-board ships; touching is allowed. Each valid
shot ends the turn. Original target 1..4 retain the recovered selector/budgets;
Prototype pursuit retains the earlier strategy. Exhausted original search can
repeat a cell with no damage; an unsupported isolated-hit trap pauses visibly.
The rotating v0.1 preset retains its older fleet/mapping/prototype opponent.
Original ship art, coordinates and persistent SMOKE damage effects remain.

Display selects default v0.4 PCX colors or recovered mutable gameplay RGB entries,
plus animated/still damage. It pauses a battle and preserves its state; Resume/P
continues. Color, motion and legacy mute preferences persist atomically in
UserData/settings.txt; unknown key/value pairs survive. Damage targets 25 Hz with
bounded catch-up, freezing on pause/focus loss/maps/still/terminal state. Palette
endpoints, visual pause/reset policy and exact global timing remain documented
modern boundaries. No automatic palette fades are enabled. Recovered maps is a
read-only inspector that pauses the duel; it does not execute scenario missions.

Evidence and limits:
WEAPONS-EVIDENCE.txt documents new source spans, lists, names, dispatch and tests.
DISPLAY-EVIDENCE, OPPONENT-EVIDENCE, PRESENTATION-EVIDENCE, CLASSIC-EVIDENCE and
FORMATS document retained systems. All native addresses/fixtures are bound to the
exact supplied Bs.exe SHA-256. Thirteen original assets match the supplied RIP.
Original reference and all earlier delivered builds are preserved.

This is a partial rebuild. Firing and targeting use isolated random streams;
the original shared layout/weapon/movie random-call schedule is not reconstructed.
Layouts, first-player choice, 600 ms response delay, immediate impact, modern
selection outlines, hit markers, persistent banners, alpha previews, pause/reset
and post-battle reveal remain modern choices. The native x87 picking boundary
precision limitation remains. Combat movie playback, transient explosion/impact
sequencing, palette fade scheduling, missions, real-time simulation, saves,
networking and controller support remain absent/provisional. No complete/exact
port or full original-battle fidelity claim is made.
User feedback confirms visible v0.5 battle effects; audible output and physical
v0.6 selection/input/focus remain separate checks. Original main-menu access and
clean Quit are confirmed; a full original gameplay comparison is pending.

Build and verification:
Build.cmd uses the existing Windows .NET Framework compiler/runtime for an x64
Forms/GDI+ game. Maintainable source separates readers, game state, targeting,
firing choices, geometry, art/effects, preferences, input/UI and audio. The app
uses bundled assets; it does not require the ISO, original registry, graphics
wrapper, Bs.exe or optional analysis tools. It installs no new runtime/engine.
Verify.cmd runs 44 focused model/reader checks and 12 actual-form checks. Numerical
sweeps, 113 targeting fixtures, 65 fade factors, 500 weapon-list outputs and 220
firing-dispatch outputs are grouped checks, not independent gameplay scenarios.
Input events are simulated offscreen. Audio format checks do not prove audibility.
The ZIP includes source, rebuild/check commands, evidence, optional tools,
provenance/integrity/portable reports and nine reviewed application previews.

Diagnostic previews (run the actual packaged executable):
--render-preview Preview.png
--render-fleet-preview Fleet-Preview.png
--render-battle-preview Battle-Preview.png
--render-small-preview Small-Preview.png
--render-legacy-preview Legacy-Preview.png
--render-map-preview Map-Preview.png
--render-damage-preview Damage-Preview.png
--render-recovered-colors-preview Recovered-Colors.png
--render-firing-preview Firing-Preview.png
Each flag takes the PNG path. Damage/color modes inject a labeled known hit and
fix visual phase for comparison. Firing mode uses labeled scripted model actions.
These are diagnostic scenes, not captures of original gameplay.

Next: decode one bounded .PAT firing clip, verify it against an independent
original decoder fixture, then establish timing, palette, completion/skip and
.SEQ audio semantics before claiming faithful combat-movie playback.