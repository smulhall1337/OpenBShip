Battleship Fleet Lab v0.13 - original cinematic HUD and viewport (2026-11-07)

Run BattleshipLab.exe or workspace outputs\Launch-Reconstruction.cmd. Before
starting a battle, enable Display -> Battle movies (experimental). Optionally
select High-detail original movies (320x240); original 160x120 stays the default.
Auto-place, Start battle, choose a target. P/Escape pauses, F2 resets, Space or
Skip stage advances once, Sound toggles, Quit exits. F2 retains settings.

Movies run behind the boards. The approved 30% transparency affects board surfaces;
grid lines/borders, ships, markers and HUD stay opaque. Full opacity returns after
completion/reset/failure; pause holds the composition. Playback controls occupy a
separate bottom row. The recovered display area is now 640x480 (4:3), cropped from
the original 640x512 PCX storage. Original E_CLAS2 panels replace the temporary HUD
rectangles. Active-side roster/number and hit/miss cards follow original phase
guards. Opaque stored black HUD backing remains a deliberate modern choice: the
native raster keys index zero transparent. See HUD-EVIDENCE.txt for exact limits.

Both detail options use original media, not AI enhancement. All 61 low-detail and
61 high-detail clips match native decoder fixtures (5641 frames per catalog).
Original per-clip duration differences are preserved. This version adds one original
HUD asset, bringing the hash-verified original asset total to 490. No source altered.

Validation: 76 model/format checks and 34 real-form checks pass. The source-only
restore, relocated ZIP, silent real-device and owned launch checks are recorded
in separate reports. INPUT-EVIDENCE.txt records12key-state/24pause-dispatch native
fixtures, with no physical keys driven. Nine full scripted v0.13duels passed;21
acrossv0.11..v0.13(including one repeated metadata regression). Production timers,
without stage skipping; Natural-Cinematic-Validation.txt records
completed runs and scope. Both61clip natural library sweeps and two prepared
near-defeat cases pass too; see Library-Validation.txt/Terminal-Validation.txt.
Physical input, audible sync and native-gameplay
comparison remain hands-on checks. Earlier v0.1..v0.12 comparisons are preserved.

This remains a partial rebuild. The modern600msopponent delay remains deliberate.
Native shared RNG scheduling, all camera/owner
routes, palette transitions, interruption and terminal timing remain incomplete.
The native admission/priority evidence is retained; the modern serial duel does
not claim native concurrent preemption. Campaigns, real-time modes, saves,
network and controllers are absent. AI clip enhancement is optional backlog SG-01.

Build.cmd uses the existing Windows .NET Framework compiler; Verify.cmd runs
focused checks. Runtime is Forms/GDI+ x64 and WinMM, with no ISO/registry/wrapper
required. Root Restore-Workspace.ps1 restores assets from the exact supplied RIP
and can regenerate original-derived evidence from guarded copied code. No SDK,
codec, AI model or engine was installed/downloaded for this milestone. Local Git
is initialized, with its initial source commit still blocked by protected metadata;
workspace outputs\Complete-Git-Checkpoint.cmd can finish it in the user's terminal.
No remote, publication or external upload was performed.



