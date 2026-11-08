Battleship Fleet Lab 0.9 — optional Classic shot movies

Run BattleshipLab.exe or outputs\Launch-Reconstruction.cmd.
Before deployment: Display -> Battle movies (experimental), then Auto-place ->
Start battle. Changing the mode resets deployment. The default remains the
accepted instant-impact model; previous v0.8 is preserved separately.

Movie-mode play:
Fire once to reserve a target. Original firing and travel clips play before
one impact changes the board. Recovered target settings 2..4 can then show
CL_HIT/CL_MISS for non-sinking outcomes. The turn hands off after feedback.
Sinks, repeated occupied targets and setting 1 follow the documented omission
rules. Overworld hit/miss/sink animation waits remain provisional.
Skip stage advances once. Space/Enter can skip while the movie/grid owns focus;
other focused native buttons retain their normal confirmation. Held keys and
mouse presses from prior stages cannot skip successive stages. P/Escape pauses,
F2 cancels/reset. Maps, library, Display and focus loss pause the pending shot;
return and Resume explicitly. Color/mute changes preserve it. Failed video loads
pause visibly for an explicit skip/reset; audio failures continue silently.
The immediate extra cannon cue is suppressed in movie mode in favor of clip cues.

The independent library now includes twenty firing clips plus six travel and two
hit/miss clips: 28 clips, 2179 frames, 117 cues, 84 WAV paths. Sound:on enables the
existing finite player and device-clock synchronization. Study controls remain
Play/Pause, restart, frame step, replay, skip and selection. Its actions do not
consume battle turns/RNG. Original/recovered palettes and damage options remain.
The rotating v0.1 preset retains its prototype and cannot enable movie turns.

Evidence and boundaries:
SHOT-EVIDENCE.txt records native addresses, stage/availability oracle, movie/HP
ordering, controls, tests and unsupported dependencies. All 2179 frame hashes and
117 cue onset/cleanup requests match controlled original-code fixtures. The native
phase function matches 528 controlled cases. Fifteen earlier/new systems remain
separate modules; this is a bounded experimental sequence, not the full original
simulation. Board impact/sink animations, native skip handling and exact shared RNG
remain unconfirmed/incomplete. Travel randomness is isolated to preserve accepted
instant-mode target AND weapon histories. Source media are never altered.

61 focused checks and 25 real-form checks pass, including original fixture grids,
delayed once-only impact, paired mode histories, terminal states, stale callbacks,
held input, paused completion, failed media, focus/maps/display, reset and layout.
UI input/failure tests are simulated; numeric grids are grouped comparisons.
Separate actual x64 audio-device checks submit SILENT PCM. No-argument hidden
agent-desktop startup/clean close also pass. Audible sound, physical controls,
perceived sync and full original-gameplay comparison remain user checks.
All 181 bundled original assets match their supplied hashes. No new SDK, engine
or codec is installed. .NET Framework x64 Forms/GDI+ and the existing finite WinMM
backend remain. Source/build/verify commands, manifests, reports, tools and fourteen
muted diagnostic previews are packaged. Previews are not original-game captures.
The game needs no original ISO, registry/wrapper, Bs.exe or optional analysis tool.

Handy comparison:
With movies off, the duel retains immediate impact. With movies on, observe that
no hit marker/HP change appears before travel ends; try pause and one-stage skips,
then reset before impact. At setting 2, compare non-sinking hit/miss feedback.
Existing mouse/grid arrows+Enter, fleet-list navigation, P/Escape, F2, mute and
Quit remain. R rotates only in the v0.1 preset. Controller support is still absent.

Project memory/version control:
PROJECT-ROADMAP.txt records SG-01: optional AI upscale from the supplied native
320x240 HWEBS versions after a separate pilot. It does not replace the fidelity
baseline. The workspace's local Git is configured for source/scripts/reports/manifests and
excludes original media, executables, ZIPs, user settings and scratch data.
The workspace-root Restore-Workspace.ps1 restores assets by hash and can regenerate
fixtures/build/checks from the supplied original RIP in a clean Windows checkout.
No repository remote/publication is requested or configured.
The repository is initialized, but its initial commit remains blocked by this
session's protected .git permissions. The workspace README describes the prepared
Complete-Git-Checkpoint.ps1 command for completing that local commit outside it.

Next: reconstruct transient board impact/sink completion callbacks (0x419CE0 /
0x4177A0 ->0x423540) and native interruption behavior before tightening phase fidelity.
Campaigns, real-time simulation, saves, networking, controllers, fades, historical
palette endpoints, shared RNG scheduling and x87 picking precision retain their
previous limits. Do not interpret continued development as full-game acceptance.
