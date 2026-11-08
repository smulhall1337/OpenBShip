Battleship Fleet Lab v0.10 - Classic hit, miss and sinking movies (2026-11-07)

Launch the workspace outputs\Launch-Reconstruction.cmd, or this folder's
BattleshipLab.exe. Windows x64; no ISO mount, registry edits, original wrapper,
Bs.exe or analysis tools required at runtime. Build.cmd uses the existing Windows
.NET Framework C# compiler. Verify.cmd runs model/format and offscreen form checks.

Before deployment choose Display -> Battle movies (experimental), Auto-place,
Start battle, then select a target. At Original target 1, firing and flight now
lead to weapon/ship-specific hit or miss playback. First hits and later hits use
separate native choices for applicable ships. All target settings can show sinking
movies, including before the final victory/defeat handoff. Settings 2..4 retain
generic CL_HIT/CL_MISS for non-sinking results. Instant mode remains default OFF
the movie path and preserves the earlier immediate-impact comparison.

P/Escape pauses, F2 resets, Skip stage/Space advances one stage, Sound toggles
playback and Quit exits. Held confirmation cannot advance successive stages;
failed video loads pause for explicit skip/reset. Maps/library/Display/focus loss
pause the duel. Return and Resume explicitly. Mouse/grid arrows+Enter and firing
ship selection remain; R rotates only in the legacy v0.1 preset. Controller absent.
The Firing clips tab exposes all 61 original combat clips for separate study.

New evidence and checks:
252 original-code selector cases cover five Classic ship types and eleven reachable
weapons, hit damage states, gun/torpedo miss variants and sinking choices. All 61
clips match 5641 original-decoder indexed-frame hashes/lengths; 404 sound cues and
232 PCM paths match fixtures. One FRG_DSH cue uses signed 16-bit PCM; its reader now
supports that explicit variant with independently tested sample values/alignment.
Native ID 12 is MIS_FLY2; ID 13 is MIS_FLY3. v0.9's travel name mix-up is corrected.
428 original asset files match the supplied RIP byte for byte. No new dependencies.
66 focused checks and 27 real-form checks pass. Actual SILENT x64 waveOut lifecycle,
owned no-argument startup/clean exit, source-only restore and relocated-package
checks supplement them. UI keys/failures are simulated; audible output, perceived
sync, physical controls and full original gameplay remain separate checks.
Eighteen muted diagnostic previews include normal/minimum impact, miss and sink
scenes. They are scripted model renders, not captures of original gameplay.

Scope:
This remains an experimental partial rebuild. Movie IDs, resource decoding and
bounded selector decisions are recovered; full camera/overlay routing, skip/fades,
movie priority, shared RNG scheduling and historical timing are incomplete. Modern
playback uses isolated impact-variant RNG, safe finite sound ownership, last-frame
holds, explicit failed-media pause and a full movie for both sides. The final game
result waits for its sink movie as a deliberate presentation policy; the original
can signal terminal state during sinking setup. Detailed confidence/limits and the
correction to the previous board-effect hypothesis are in IMPACT-EVIDENCE.txt.

Earlier v0.9 and older packages/ZIPs remain preserved. PROJECT-ROADMAP.txt retains
SG-01 AI upscaling as an optional future HWEBS pilot; no upscale was performed.
The workspace-root README and Restore-Workspace.ps1 document source restoration.
Local Git main is initialized but its initial commit remains pending protected
metadata writes. No remote or publication. Complete-Git-Checkpoint.cmd in the
workspace outputs folder can finish the prepared source snapshot outside that
restriction. Original media/builds/settings/scratch remain excluded from Git.

Next comparison: enable movies at Original target 1, observe an ordinary miss,
a non-sinking hit and a sink; verify the next turn waits, plus pause/skip/F2.
Confirm audible playback separately. Next reconstruction: trace original movie
priority/interruption and Classic completion/terminal routing before adjusting
those behaviors. Preserve instant mode and isolated RNG until shared scheduling
has evidence. Campaigns, real-time modes, saves, networking and controllers absent.
