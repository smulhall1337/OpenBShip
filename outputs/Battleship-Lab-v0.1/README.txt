Battleship Fleet Lab 0.1

This is the first playable reconstruction prototype, with original board art, a recovered ship sprite, original sound clips and experimental Classic rules. It is not a completed port of the supplied game.

Run BattleshipLab.exe, or use Launch-Reconstruction.cmd in the parent outputs folder. The prototype uses its bundled assets and the existing Windows .NET Framework. It does not use the original game's launcher, registry settings, mounted ISO or cnc-ddraw library.

Quick start:
1. Choose Auto-place, then Start battle.
2. Click a square on the upper-right board to fire.
3. The opponent takes one shot, then your turn returns.
4. Sink all five opposing ships to win. Use New battle to restart.

For manual deployment, click the lower-left board to place the five ships in order. R changes orientation. Ships cannot overlap or extend beyond the board; touching ships are allowed.

Controls:
- Mouse: place a ship or fire at a square.
- Arrow keys: move the highlighted square; Enter places/fires.
- R: rotate during placement.
- P or Escape: pause/resume during battle.
- F2: new battle, returning to deployment.
- Sound button: mute/unmute; this preference is saved under UserData.
- Quit or the window close button: exit.
The game pauses when it loses focus. Resume with P or the Resume button. Held keyboard controls are suppressed across focus transitions until released.

Recovered maps is a separate inspection tab. It displays three decoded 64x64 original scenario grids with schematic colors. It pauses an active battle; return to Classic duel and use Resume to continue. It does not execute their missions or simulate their entities.

The rule assumptions are explicit: fleet lengths 5/4/3/3/2, one shot per turn even after a hit, touching ships permitted, no time limit, and defeat when all 17 ship cells are hit. The opponent uses random search and adjacent-square pursuit after a hit. These rules and the opponent strategy are provisional modern code; the original executable's gameplay logic has not been reconstructed or calibrated against a full original battle. Placement markers, hit symbols, board picking and the surrounding interface are also new code. The original movies, animations, mission scripts, real-time fleet simulation, multiplayer and save games are absent.

The eight packaged original asset files are byte-identical copies of the supplied Full-RIP files. Asset hashes are in Asset-Provenance.csv. The original application remains available through outputs\Launch-Original.cmd outside this package.

Source code is in Source. Resources, game state, presentation and audio are separate modules. MainForm wires the controls and input; Program owns startup and diagnostic modes. Build.cmd rebuilds the Windows x64 executable with the .NET Framework C# compiler already present on this machine. No package download is required. Tools\Build-Tools.cmd rebuilds the asset inspection tool.

Reproducible checks, from this folder:
BattleshipLab.exe --self-check Self-Check.txt
BattleshipLab.exe --render-preview Preview.png
BattleshipLab.exe --render-battle-preview Battle-Preview.png
BattleshipLab.exe --render-map-preview Map-Preview.png

The preview commands briefly create the application's own offscreen window without activation, render it, close it and return. They do not capture another application. Fourteen focused checks pass in the delivered build, including independent binary fixtures, malformed-input rejection, placement/combat/terminal states, a complete deterministic battle, and board anchors checked against the original border pixels. This does not establish audible playback, desktop keyboard/focus behavior or original gameplay fidelity. Those need hands-on comparison.

Self-Check.txt records results. Preview.png, Battle-Preview.png and Map-Preview.png show rendered layouts. FORMATS.txt records recovered structure, evidence and limitations. If startup fails, a managed error report is written to UserData\last-error.txt when that directory is writable.

Next useful comparison: play a few turns here and in the original game's Classic mode. Check ship lengths, placement restrictions, whether hits keep the turn, sink feedback and how the original handles repeated shots. Those observations will replace the provisional rules one at a time.
