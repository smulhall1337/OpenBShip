Battleship reconstruction - playable v0.31 / checkpoint 41

Run outputs/Launch-Reconstruction.cmd. This is a runnable partial rebuild for
modern Windows x64 using the existing .NET Framework, WinForms, GDI+ and WinMM.
Build.cmd and Verify.cmd are in outputs/Battleship-Lab-v0.31.

New: repeated shots at sunk-ship cells now receive the original miss feedback
without new damage. Repeated hits on surviving ships consume no feedback random
draw. Evidence comes from executing the original phase, impact, feedback and
sink-cleanup code in owned numerical helpers. The playable v0.30 remains available.

Classic route: Original menu > Solo > Classic > Start Game > Fleet Configuration
> Start Game > loading > battle. Auto-place and Start also work directly. P/Escape
pauses, F2 resets, mouse/arrows+Enter targets, Space/Skip advances a movie stage.
Movies stay behind the board with approved 30% surface transparency; ships, grid,
markers and HUD remain opaque. Existing settings and bindings are preserved.
Original intro replay, Preferences/Credits, Quit, result movies and the 137-movie
archive remain. Source evidence and deliberate modern choices are documented.

Current production checks: 137 model/format + 88 real-form = 225 focused groups,
plus 48 shutdown cases. The separate damage comparison matches 2,496 bounded
native cases. A controlled shot plays firing/travel/feedback through production
timers and SILENT device buffers, pauses, ends its turn and closes cleanly.
These checks do not establish audible output, physical input or full native play.
Read outputs/Current-Status.txt and Native-Damage-Order-Findings.txt for scope.

Fresh source-only restore, with the exact supplied RIP:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Workspace.ps1 -OriginalRoot "<supplied RIP root>" -WithChecks -AudioDeviceCheck
The modern game requires no original EXE, mount or registry configuration to run.
Research tools require the exact supplied Bs.exe. Source checkpoint/hash record:
outputs/Source-Checkpoint-41.txt. Original game media is excluded from source ZIPs.

Per user instruction, automatically commit each completed tested checkpoint locally.
AGENTS.md records the policy. Complete-Git-Checkpoint.ps1 accepts -Message and
records commit identity in ignored outputs/Git-Checkpoint.txt. No push is requested.

Ten tested builds are retained, v0.22 through v0.31. Older generated copies retire
after verified source/report/settings archival. Original material, reference setup,
authoring/Git history and source snapshots remain. See Build-Cleanup-Summary.txt.

Still incomplete: original live-gameplay reference, complete lifecycle and shared
RNG schedule, campaign/network/tutorials/radar, controllers and save formats.
Original reference acceptance is main menu and clean Quit only. AI movie upscale
remains stretch goal SG-01; no model download or processing has occurred.
