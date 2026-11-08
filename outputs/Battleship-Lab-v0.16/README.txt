Battleship Fleet Lab v0.16 - original title/menu handoff - 2026-11-07

Run BattleshipLab.exe or workspace outputs\Launch-Reconstruction.cmd. Open the
Original menu tab, or let a Result sequence finish: both outcomes now return
to the supplied title screen. Its original artwork, selected-label sprite halves,
button positions and click boundaries are reconstructed. Arrow keys select;
Enter/Space or a fresh mouse click activates. Replay entry repeats the fade.
Pause/Resume fade works, and focus loss/tab changes pause. F2 resets everything.

Modern bridges are stated above the artwork: Start New Solo Game starts the
current modern duel setup, retaining its selected rules/opponent/display choices.
Preferences opens current display settings. Multiplayer reports that it is not
implemented. Quit Battleship opens an inline modern confirmation; Cancel stays
in the menu and Quit closes Fleet Lab. Return to duel preserves the paused board.
Original solo setup, network lobby, full preferences and quit-card scripts are
not executed. These bridges are intentional modernization, not recovered original
submenu behavior. Opening the title alone does not discard a battle. The default
startup still opens the existing Classic duel, preserving the accepted workflow.

Reconstruction: original ending opcode18 targets44EF88; entry initializes native
front-end values and chains to44EFC0. Exact56/168byte entry/title prefixes are
validated, stopping at input state39. Four action IDs/coordinates and8split-sprite
draw commands match original code.1104grouped boundary samples and four complete
indexed raster compositions match guarded native execution. The left/right halves
are200/174pixels; native click width is400, and index0 is transparent. Native
fade factors12..64 are retained.40ms/factor is a documented modern cadence;
the native outer-loop timing is unmeasured. Native reset values are evidence;
the modern game deliberately retains its paused board until Solo/F2 is selected.

88model/format+46form=134focused checks and14shutdown cases pass. Source/portable,
natural handoff, SILENT device and owned launch scopes are in validation reports.
SILENT playback is device/lifecycle evidence, not audible sync. Physical desktop
input/focus remains a hands-on check. The v0.15.1exit fix is retained, with extra
late-tab/key/view guards and menu/confirmed-Quit shutdown coverage. Approved30%
board-surface transparency and opaque overlays remain unchanged. No bindings,
original media or user settings are rewritten by diagnostics.

Build.cmd/Verify.cmd use installed .NET Framework, Forms/GDI+ x64 and WinMM.
Restore-Workspace.ps1 restores527hash-verified original files and exports four
bounded runtime scripts even without -WithChecks. WithChecks regenerates the
native-derived evidence from exact supplied Bs.exe. Optional analysis oracles
execute copied hash-guarded fragments with recording stubs; production executes
no original machine code. No engine/model/dependency was downloaded. Earlier
builds/reference are preserved. This is still a partial rebuild; see MENU-EVIDENCE.txt.
AI enhancement remains optional SG-01 backlog, with no processing performed.
