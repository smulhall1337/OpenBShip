Battleship Fleet Lab 0.3 — original Classic ship art and board coordinates

Launch BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Choose Auto-place -> Start battle, then fire on the upper-right board.
P pauses/resumes; F2 returns to a fresh deployment in the current preset.

Newly playable in v0.3:
- Recovered Classic uses the original screen projection and X/Y orientation.
  The earlier prototype's axes were rotated relative to the native code. This
  corrects the displayed fixed-axis direction and the selected grid coordinates.
- Original ship sprites replace filled-cell ship placeholders in Recovered
  Classic: Super Carrier, Battleship, Frigate, Missile Submarine and Destroyer.
  Native frame selection, 79% scaling and odd/even anchor offsets are recovered.
- Manual placement centers each ship on the selected cell, as the original
  placement validator/commit paths do. A translucent ship and colored footprint
  preview the result. Overlap and board-edge checks still apply.
- Original HIT/MISS sprite artwork reports each board's last shot result.
- Hidden opponent ships stay hidden during battle. Sunk ship art disappears;
  the original red fleet-status icon and modern cell-hit markers remain.

Quick manual comparison:
Clear fleet, then click the lower-left board at each ship's center. Ships follow
one fixed axis. The selected center must leave room for the whole ship. A 5-cell
ship, for example, needs two cells on either side of the selected Y index.
For a quick duel, use Auto-place and Start battle. Each valid shot ends the turn.
Repeated shots are rejected. Sink all five opposing ships to win.

The default recovered fleet has 19 cells: Super Carrier 5, Battleship 5, Frigate 3,
Missile Submarine 4, Destroyer 2. The selectable v0.1 prototype preset keeps the
old 17-cell fleet, rotation and previous board mapping. Preset changes clear the
deployment and are locked during battle. The separate v0.1 and v0.2 packages are
preserved. Parent Launch-Reconstruction-v0.1.cmd / -v0.2.cmd open those builds.

Controls:
Mouse places/fires. Arrow keys move the logical grid cursor; Enter places/fires.
R rotates only in the v0.1 preset. P/Escape pauses/resumes, F2 restarts, Sound
mutes/unmutes, Quit exits. Focus loss pauses a battle. Resume explicitly afterward.
Held confirmation keys are suppressed until released across focus transitions.
Sound preference is saved in UserData/settings.txt. No new bindings replace old
ones. The preset selector uses normal keyboard navigation while focused.

Recovered maps is a separate read-only tab with three original 64x64 scenario
grids. Opening it pauses a battle. It does not execute missions or entities.

Evidence and limits:
PRESENTATION-EVIDENCE.txt documents this checkpoint's native addresses, formulae,
integer rounding, frame mapping, transparency and remaining precision limits.
CLASSIC-EVIDENCE.txt records fleet/placement/shot evidence. Evidence/ contains raw
extracted code/table bytes and CSV provenance tied to the exact supplied Bs.exe.
The twelve bundled asset files match the user-supplied RIP by SHA-256.

This remains a partial reconstruction. Native fleet definitions, basic shot rules,
placement constraints, normal solo turn handoff, ship frames, scaling and board
axes are evidenced from original code. The new AI, layout random sequence,
first-player choice, 600 ms opponent delay, pause/reset, surrounding UI and cell
markers remain modern design. Result banners persist until the next shot, rather
than following the original cinematic phase schedule. Surviving opponent ships
are revealed only after a finished battle as a modern convenience.

The source inverse uses x87 extended intermediates; v0.3 preserves the known
float32 store and truncation toward zero, with double approximating the other
operand. Exact floating-point boundary ties are not claimed bit-identical.
The accepted PCX palette appearance is retained; complete native palette state,
original AI, combat movies/fire animation, weapon presentation, missions, real-time
fleet simulation, multiplayer, saves and controller support remain absent.
Original full-gameplay comparison, audible sound and physical input/focus behavior
still need hands-on checking. No claim of a complete or exact port is made.

Build.cmd uses the existing Windows .NET Framework C# compiler for Windows x64
Windows Forms/GDI+. Source separates readers, model, Classic geometry, native ship
presentation, generic rendering, audio, UI/input, validation and startup. No new
runtime or engine installation is needed. The game runs from its own assets and
does not use the original registry keys, mounted disc or cnc-ddraw wrapper.

Verify.cmd runs 26 focused model/reader checks and 5 real-form checks. Sweeps of
400 board centers and 128 layout seeds per preset are numeric cases within those
checks, not hundreds of separate gameplay scenarios. Form tests use real handlers
with simulated input, including mouse placement at two sizes. Previews render the
actual application's own offscreen form and close it; they do not capture another
application. The six inspected preview scenes use a stable comparison seed.

Diagnostic modes:
BattleshipLab.exe --self-check Self-Check.txt
BattleshipLab.exe --ui-check UI-Check.txt
BattleshipLab.exe --render-preview Preview.png
BattleshipLab.exe --render-fleet-preview Fleet-Preview.png
BattleshipLab.exe --render-battle-preview Battle-Preview.png
BattleshipLab.exe --render-small-preview Small-Preview.png
BattleshipLab.exe --render-legacy-preview Legacy-Preview.png
BattleshipLab.exe --render-map-preview Map-Preview.png

Asset-Provenance.csv, Dependency-Provenance.csv and Build-Manifest.csv record the
delivered files. Tools/ includes read-only, hash-guarded evidence extractors and
an optional MIT-licensed Iced-based disassembler; the game does not load Iced.dll.
The accepted original comparison remains available through Launch-Original.cmd.

Next useful hands-on check: deploy a ship by its center, fire several shots,
check the visible footprint against the ship, then pause, restart and quit.
Compare the original Classic placement direction and ship art. Next static work:
trace the native opponent targeting and palette changes to replace those remaining
provisional systems with separately evidenced implementations.
