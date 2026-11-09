Battleship Fleet Lab v0.21 - original Classic pause typography - 2026-11-07

Run BattleshipLab.exe or workspace outputs\Launch-Reconstruction.cmd.
Start a Recovered Classic battle through Original menu > Solo > Classic >
Start Game > Fleet Configuration > Start Game, or use Auto-place > Start battle.
The loading card, ready/fire/sink messages, native HUD and combat movies remain.

P or Escape pauses/resumes battle. The pause banner now uses the original larger
sprite font and centered layout. Its middle line says Escape Also Resumes to
match these accepted controls. Native Escape-to-quit/options behavior is not
implemented. Resume button also works. F2/New battle resets; Quit exits.
Arrows move the board cursor; Enter selects/fires; mouse targets cells. Space/
Skip stage advances a cinematic once. Firing fleet selection chooses the shooter.
Existing menu/fleet/loading controls remain separate; Escape cancels loading.

Movies appear behind the board; cinematic board surfaces remain30percent
transparent with opaque grids/ships/markers/HUD/text. Display controls colors,
motion and low/high movie detail; choose movie options before a battle. Sound
is independently configurable. Existing settings are copied into this version.

Build.cmd / Verify.cmd use the existing Windows .NET Framework compiler; x64.
102model/format +58form groups =160focused checks;20shutdown cases pass.
New pause fixtures compare source centering, style4measurement, spacing and
indexed pixels. Real form checks preserve P/Escape edges, movie pause and reset,
and release both font caches on palette changes. See Pause-Validation.txt and
PAUSE-EVIDENCE.txt for source identity, exact scope, reconstruction/modernization
split and limitations. Other evidence reports retain their named earlier scope.

This is a partial rebuild. Native live battle behavior, remaining modes/campaign,
saves/network/controllers, general menu typography and full native lifecycle
remain unfinished. AI movie enhancement stays optional stretch goal SG-01.
Normal/minimum previews are pause-Preview.png and pause-small-Preview.png.
Next hands-on comparison: pause during a battle movie, read all three lines,
resume with P/Escape, and quit. Automation does not establish audible playback
or physical input acceptance. Older builds and the original reference are kept.