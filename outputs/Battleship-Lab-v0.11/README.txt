Battleship Fleet Lab v0.11 - cinematics behind translucent boards (2026-11-07)

Run BattleshipLab.exe or workspace outputs\Launch-Reconstruction.cmd.
Before deployment: Display ->Battle movies (experimental), Auto-place, Start battle.
Movies now fill the background while both boards, ships, hit/miss markers, fleet
panels and controls remain visible in the foreground. The user approved 30% board
surface transparency during cinematics. Surfaces use 70% opacity; native grid-line
pixels, borders, ships, markers and HUD remain opaque. Full opaque artwork returns
when the cinematic completes, resets or fails to load. Pause freezes the same
translucent frame; playback controls occupy a separate bottom row without overlap.

This is an intentional modern presentation, not a claim that the original used
this alpha value/mask. Video keeps its aspect ratio with centered cropping to fill
the playfield. Original PCX/PAT/PAL assets remain unchanged. Original movie camera,
owner routing, priority and interruption are still under investigation.
The earlier full-field presentation remains runnable as v0.10 for comparison.

Recovered content remains the v0.10 slice: 61 clips, 5641 native frame fixtures,
404 sound cues, 232 PCM paths and 428 original asset files. Default target setting1
has ship/weapon-specific hit/miss clips; all settings have sinking clips. Settings
2..4 use CL_HIT/CL_MISS for non-sinking feedback. Instant mode remains available.
P/Escape pauses, F2 resets, Skip stage/Space advances one stage, Sound toggles and
Quit exits. Held confirmation and stale/reset/failure guards are preserved. Maps,
library, Display and focus loss pause the duel; Resume explicitly on return.

68 model/format checks and 30 real-form checks pass, including synthetic red/green
backdrop composition at normal/minimum sizes, surface alpha/blend values, opaque
grid/HUD pixels, no original-data modification, non-overlapping controls, palette
replacement lifetime, completion/pause/reset/failure restoration and existing
simulation/media/audio/input regressions. UI input is simulated offscreen.
Separate source restore, relocated ZIP, actual SILENT x64 waveOut lifecycle and
owned no-argument startup/clean exit0 checks pass. Sound perception, physical
controls, exact native game behavior and visual acceptance of this new layout
remain user checks. Twenty muted diagnostic previews are scripted renders.

Build.cmd/Verify.cmd use existing Windows .NET Framework Forms/GDI+ x64 and WinMM.
Source, provenance, evidence/tools and reports are included. No new dependencies,
network uploads or AI processing. No ISO/registry/wrapper/Bs.exe is used at runtime.
BACKDROP-EVIDENCE.txt documents approved transparency and the modern mask. Earlier
IMPACT/AUDIO/SHOT evidence remains historical subsystem reference with its limits.
AI upscaling remains optional roadmap SG-01. Controllers/full campaigns/real-time
modes/saves/networking remain absent. This is a partial rebuild.

Local Git is initialized; initial commit still awaits writable .git metadata.
No remote/publication. Workspace outputs\Complete-Git-Checkpoint.cmd finishes
that prepared source snapshot outside the restriction. Original media/builds/user
settings/scratch stay excluded. Restore-Workspace.ps1 supports source-only rebuild.
Next: inspect original movie priority/interruption and completion/terminal routing.
