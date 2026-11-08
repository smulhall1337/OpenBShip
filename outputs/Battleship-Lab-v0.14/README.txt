Battleship Fleet Lab v0.14 - original ending movie library - 2026-11-07

Run BattleshipLab.exe or workspace outputs\Launch-Reconstruction.cmd. In Combat
movies, choose VICTORY or LOSE and press Play. Both use their supplied 320x240
originals at the recovered 20fps script cadence. Pause, Restart, Previous/Next
frame and Skip to end remain available. Sound controls the original cues. Leaving
the tab, opening Display or losing focus pauses playback. F2 resets the player
and deployment. These longer endings are study playback; automatic native ending
scripts/cards/menu return are not yet reconstructed in the duel.

The library now has 63 movies: 61 existing combat clips at25fps and two endings
at20fps. The endings have no supplied low-detail versions, so they stay320x240
regardless of the combat-quality preference. No low ending or AI detail is invented.
VICTORY has661frames(33.05seconds); LOSE680frames(34seconds). All1341ending frame
hashes and consumed lengths match controlled original-code decoding.60cue onsets/
cleanup and26PCM paths are checked. The supplied LOSE README describes optional
music; it and all original TUNE cues are preserved unchanged.

The player now creates only its displayed colour bitmap, retaining the outgoing
one until the synchronous frame handoff completes. Decoded index surfaces remain
in memory; this is not a streaming PAT decoder. A high-detail ending retains
about49.8MiBof indexed pixels rather than allocating roughly199MiBof colour
bitmaps for every frame. Whole-process memory also includes loading/audio/GC.
The mixer uses exact frame-to-sample floor conversion for20fps at11025Hz. A finite
buffer ending between sample boundaries now completes the last visual frame
without incorrectly reporting a stalled audio device.

The Classic duel keeps its accepted behavior: optional background combat movies,
approved30%board-surface transparency, opaque grids/ships/markers/HUD, original
4:3viewport and restored opacity after playback. Original HUD and battle media
are preserved. Finished guidance now points to ending study and F2; the tab name
reflects its wider library. No accepted bindings or user configuration are changed
by diagnostics; form checks now initialize their own unsaved display defaults.

81model/format and38real-form checks pass. Source-only, portable, production-startup
and natural-playback results are recorded separately. Tests with SILENT audio
exercise the real Windows device, not audible output/perceived sync. Offscreen
input remains simulated. General v0.13appearance was accepted by the user; new
ending perception/audio and complete native gameplay remain hands-on checks.
This is a partial rebuild: native shared RNG/ownership, fades/interruption, whole
ending scripts, campaigns, saves/networking/controllers and other modes are absent
or incomplete. AI enhancement remains optional backlog SG-01 and has not run.

Build.cmd and Verify.cmd use the installed Windows .NET Framework compiler,
Forms/GDI+ x64 and WinMM. Root Restore-Workspace.ps1 restores hash-verified assets
from the supplied exact RIP and can regenerate original-derived fixtures. No SDK,
engine, codec or model was downloaded/installed. Earlier runnable versions and
the original menu/Quit reference are preserved. See ENDINGS-EVIDENCE.txt for source
addresses, confidence, timing, memory ownership and implementation boundaries.

