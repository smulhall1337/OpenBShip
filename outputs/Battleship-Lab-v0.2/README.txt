Battleship Fleet Lab 0.2 — recovered Classic fleet

Run BattleshipLab.exe, or Launch-Reconstruction.cmd in the parent outputs folder.
Choose Auto-place, then Start battle. Fire on the upper-right board. Sink all
five opposing ships to win. P pauses/resumes; F2 starts a new deployment.

New in this checkpoint:
- Recovered Classic is the default preset. The original executable creates a
  Super Carrier (5 cells), Battleship (5), Frigate (3), Missile Submarine (4),
  and Destroyer (2): 19 occupied cells. Its traced placement paths keep X fixed
  and extend along Y, reject overlap/board edges, and allow touching ships.
- Original EO_CLASS.SPH fleet icons are drawn at recovered coordinates. A sunk
  ship switches to the original red destroyed icon.
- The v0.1 prototype preset retains the earlier 17-cell fleet and rotation.
  Choose it in the right sidebar during deployment. Changing preset clears the
  deployment. Preset changes are disabled during battle; F2 retains the preset.
- The sidebar keeps the shot log visible at the minimum supported window size.

Manual deployment: click the lower-left board to place each listed ship. The
new default follows the original fixed axis. The v0.1 preset permits R to rotate.
Auto-place generates a fresh valid layout in the selected preset.

Controls:
- Click: place a ship or fire at an untried square.
- Arrow keys and Enter: move the board cursor and place/fire.
- R: rotate in the v0.1 preset; fixed-axis deployment in Recovered Classic.
- P or Escape: pause/resume in battle. Focus loss also pauses.
- F2: new battle/deployment in the current preset.
- Sound: toggle effects; saved locally under UserData/settings.txt.
- Quit or the window close button: exit.
The preset selector supports standard keyboard navigation when it has focus.
Held board confirmation keys are suppressed until released after activation.

Recovered maps remains a separate, read-only inspection tab. It pauses an active
battle and displays three decoded original scenario grids with schematic colors.
Return to Classic duel and Resume to continue. No scenario missions are executed.

Evidence and limits:
This remains a partial reconstruction. Fleet identities, lengths, initial hit
counts, fixed-axis placement, touching/overlap rules, basic hit/repeat/sink rules
and normal solo turn progression were traced in the supplied executable. Read
CLASSIC-EVIDENCE.txt for addresses, raw source fixtures and confidence limits.
The fleet has not yet been compared through a complete desktop original battle.

Opponent targeting, random layout sequences, 600 ms response delay, board picking,
placement/hit markers, pause behavior, and surrounding controls are new code.
The AI searches randomly and pursues adjacent cells after hits; it receives no
hidden ship positions when choosing a shot. Original movies, combat animation,
weapon presentation, original AI, missions, real-time fleet simulation, networking,
saves and controller input are absent. Palette transitions and exact board input
projection remain unresolved. There is no claim of a completed or exact port.

The package runs independently of the original registry keys, mounted ISO and
graphics wrapper. The accepted original launcher remains Launch-Original.cmd.
The accepted v0.1 folder and ZIP are preserved; Launch-Reconstruction-v0.1.cmd
in the parent folder opens that earlier build.

Rebuild and validate:
Build.cmd uses the existing Windows .NET Framework compiler to create an x64
Windows Forms/GDI+ executable. Source separates assets, game state, presentation,
audio, UI/input, validation and startup. No engine or SDK download is needed.
Verify.cmd runs the delivered executable's 20 focused checks and 4 form checks.
The 128-seed sweep per preset and 200 board centers are numeric sweeps within
those checks, not hundreds of distinct gameplay scenarios.

Diagnostic modes:
BattleshipLab.exe --self-check Self-Check.txt
BattleshipLab.exe --ui-check UI-Check.txt
BattleshipLab.exe --render-preview Preview.png
BattleshipLab.exe --render-battle-preview Battle-Preview.png
BattleshipLab.exe --render-small-preview Small-Preview.png
BattleshipLab.exe --render-legacy-preview Legacy-Preview.png
BattleshipLab.exe --render-map-preview Map-Preview.png

Form checks and previews create the actual application's own offscreen window,
then close it. They do not capture another application. Key checks are simulated;
physical input, audible sound and full original gameplay fidelity remain separate
hands-on checks. Preview images were visually inspected at 1236x879 and 1040x780.

Asset-Provenance.csv records the nine byte-identical supplied assets.
Evidence/ contains extracted original table/code bytes, with source addresses and
hashes. Tools/Extract-ClassicEvidence.ps1 reproduces them only for the matching
original executable hash. Tools/README.txt describes the reusable analysis tools
and their optional MIT-licensed Iced disassembler dependency. The game itself does
not load Iced.dll. Build-Manifest.csv covers package files except mutable UserData.

Next comparison: Auto-place and play a battle in Recovered Classic; check the
19-cell fleet, fixed-axis deployment, one-shot turns and red sunk-ship icons.
Then compare the original Classic screen's placement direction and shot feedback.
User feedback that v0.1 "looks good" is retained as appearance acceptance; it is
not recorded as confirmation of a completed battle or audible playback.
